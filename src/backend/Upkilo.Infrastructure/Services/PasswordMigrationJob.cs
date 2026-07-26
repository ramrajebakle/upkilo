using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// One-shot background job that runs once at startup to detect users whose stored password
/// hash is not BCrypt (plain-text, MD5, SHA-1, SHA-256, or BCrypt with cost &lt; 12) and
/// forces a password-reset email to each affected account.
///
/// WHY: We cannot rehash a password without the plaintext. The correct migration strategy is:
///   1. This job identifies and notifies at-risk accounts on startup.
///   2. AuthService.LoginAsync upgrades the hash transparently on the user's next login.
///      (Legacy hash → BCrypt/12; low-cost BCrypt → BCrypt/12.)
///   3. Accounts that never log in keep receiving the reset email on every deployment until
///      they click the link and set a new password (which stores BCrypt/12).
///
/// RUNNING IN PRODUCTION:
///   Set  PasswordMigration:Enabled = true  in appsettings.Production.json (default: false).
///   Disable again after the first wave of emails has been sent (one run is enough per batch).
/// </summary>
public class PasswordMigrationJob : BackgroundService
{
    private const int TargetBCryptCost = 12;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PasswordMigrationJob> _logger;

    public PasswordMigrationJob(IServiceScopeFactory scopeFactory, ILogger<PasswordMigrationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short startup delay so the rest of the app (DB migrations etc.) has settled
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var scope = _scopeFactory.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Guard — must be explicitly opted-in; off by default
        if (!config.GetValue<bool>("PasswordMigration:Enabled"))
        {
            _logger.LogInformation("[PasswordMigration] Disabled (PasswordMigration:Enabled = false). Skipping.");
            return;
        }

        _logger.LogInformation("[PasswordMigration] Starting password-hash audit...");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var appUrl = config["APP_URL"] ?? "https://app.upkilo.com";

        // Stream users in pages of 1000 to avoid loading all hashes into memory at startup.
        int legacy = 0, lowCost = 0, skipped = 0;
        const int pageSize = 1000;
        int offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
        var users = await db.Users
            .IgnoreQueryFilters()
            .Where(u => !u.IsDeleted && u.SocialProvider == null)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.PasswordHash })
            .OrderBy(u => u.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(stoppingToken);

        if (users.Count == 0) break;
        offset += users.Count;

        foreach (var user in users)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var hash = user.PasswordHash ?? "";

            bool isLegacy = !hash.StartsWith("$2", StringComparison.Ordinal);
            bool isLowCost = !isLegacy && IsLowCostBCrypt(hash);

            if (!isLegacy && !isLowCost)
            {
                skipped++;
                continue;
            }

            if (isLegacy) legacy++;
            else lowCost++;

            _logger.LogWarning("[PasswordMigration] Weak hash detected for User {UserId} ({Type})",
                user.Id, isLegacy ? "non-BCrypt" : $"BCrypt cost<{TargetBCryptCost}");

            try
            {
                // Initiate a standard password-reset flow for this user
                await authService.InitiatePasswordResetAsync(user.Email);

                _logger.LogInformation("[PasswordMigration] Password reset email dispatched to {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PasswordMigration] Failed to dispatch reset email for {UserId}", user.Id);
            }

            // Brief pause between emails to avoid overwhelming the mail relay
            await Task.Delay(200, stoppingToken);
        }
        } // end page loop

        _logger.LogWarning(
            "[PasswordMigration] Audit complete. Legacy={Legacy}, LowCostBCrypt={LowCost}, Compliant={Skipped}",
            legacy, lowCost, skipped);
    }

    /// <summary>
    /// Returns true when the BCrypt cost factor is below the target.
    /// BCrypt hash format: $2b$COST$[22-char salt][31-char hash]
    /// </summary>
    private static bool IsLowCostBCrypt(string hash)
    {
        var parts = hash.Split('$');
        return parts.Length >= 3 && int.TryParse(parts[2], out var cost) && cost < TargetBCryptCost;
    }
}
