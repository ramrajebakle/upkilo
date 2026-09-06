using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Background job that sends onboarding drip emails to tenants
/// who haven't completed setup after 7 days.
/// Runs once daily, skips already-emailed tenants.
/// </summary>
public class OnboardingDripJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OnboardingDripJob> _logger;

    public OnboardingDripJob(IServiceScopeFactory scopeFactory, ILogger<OnboardingDripJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnboardingDripJob encountered an error");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    /// <summary>
    /// One pass of the job. Public so it can be exercised directly: the only other entry point is
    /// ExecuteAsync's infinite loop with a 24-hour delay in it, which is not a thing a test can
    /// drive without racing the scheduler.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Was hardcoded to https://app.upkilo.com, so every non-production environment mailed
        // people a link into production.
        var appUrl = (configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var fourteenDaysAgo = DateTime.UtcNow.AddDays(-14);

        // Find tenants created 7-14 days ago with incomplete onboarding who haven't been nudged
        var candidates = await context.Set<TenantOnboardingProgress>()
            .Include(p => p.Tenant)
            .Where(p =>
                p.CreatedAt >= fourteenDaysAgo &&
                p.CreatedAt <= sevenDaysAgo &&
                !p.IsDismissed &&
                p.DripEmailSentAt == null &&
                // At least one step incomplete
                (!p.BusinessProfileCompleted ||
                 !p.ServicesAdded ||
                 !p.StaffAdded ||
                 !p.FirstBookingCreated))
            .ToListAsync(ct);

        _logger.LogInformation("OnboardingDripJob: {Count} tenants to nudge", candidates.Count);

        // One query for every recipient rather than one per tenant inside the loop. Also supplies
        // the greeting name: the email used to open "Hey {tenant.Name}", which is the COMPANY
        // name — "Hey Acme Dental Ltd! 👋".
        var tenantIds = candidates.Select(p => p.TenantId).ToList();
        var owners = await context.Users
            .IgnoreQueryFilters()
            .Where(u => tenantIds.Contains(u.TenantId) && !u.IsDeleted
                        && (u.Role == UserRole.Owner || u.Role == UserRole.Admin))
            .OrderBy(u => u.Role).ThenBy(u => u.CreatedAt)
            .Select(u => new { u.TenantId, u.Email, u.FirstName })
            .ToListAsync(ct);

        var ownerByTenant = owners
            .GroupBy(u => u.TenantId)
            .ToDictionary(g => g.Key, g => g.First());

        // The ninth step, ai_copilot_quickwin, has no stored flag — OnboardingController detects
        // it from AI usage. Without this the email can only ever reach 8/9 and would tell a fully
        // set-up tenant they are 89% done.
        var tenantsWithAiUsage = (await context.AIUsageLogs
            .IgnoreQueryFilters()
            .Where(a => tenantIds.Contains(a.TenantId))
            .Select(a => a.TenantId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        foreach (var progress in candidates)
        {
            var tenant = progress.Tenant;
            if (tenant == null) continue;

            ownerByTenant.TryGetValue(progress.TenantId, out var owner);

            // Tenant.Email is the intended address, but registration never populated it, so it is
            // null for every tenant created before that was fixed — and this `continue` silently
            // skipped all of them, which is why the nudge has never reached a single customer.
            // The owning user's address is the same person and is always present.
            var recipient = !string.IsNullOrWhiteSpace(tenant.Email) ? tenant.Email : owner?.Email;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                _logger.LogWarning("No email address for tenant {TenantId}; skipping drip", progress.TenantId);
                continue;
            }

            var greetingName = !string.IsNullOrWhiteSpace(owner?.FirstName) ? owner!.FirstName : "there";

            var completedCount = CountCompleted(progress)
                                 + (tenantsWithAiUsage.Contains(progress.TenantId) ? 1 : 0);
            var pct = (int)((completedCount / (double)TotalSteps) * 100);

            var nextStepHint = GetNextStepHint(progress);

            try
            {
                await emailService.SendSystemEmailAsync(
                    recipient,
                    "You're almost there! Finish setting up Upkilo",
                    $@"<h2>Hey {greetingName}! 👋</h2>
                       <p>You're <strong>{pct}% done</strong> setting up your Upkilo account — just a few steps left.</p>
                       <p>Your next step: <strong>{nextStepHint}</strong></p>
                       <p>Completing setup takes less than 5 minutes and unlocks your first bookings.</p>
                       <p><a href='{appUrl}/onboarding' style='background:#6366f1;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;'>Continue Setup →</a></p>
                       <p style='color:#6b7280;font-size:12px;margin-top:24px;'>You're receiving this because you signed up for Upkilo. <a href='{appUrl}/settings/notifications'>Manage preferences</a></p>");

                progress.DripEmailSentAt = DateTime.UtcNow;

                _logger.LogInformation("Drip email sent to tenant {TenantId}", progress.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send drip email to tenant {TenantId}", progress.TenantId);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Must match the number of steps OnboardingController publishes, or the percentage in this
    /// email disagrees with the percentage on the page it links to. It said 8 while the checklist
    /// served 9.
    /// </summary>
    private const int TotalSteps = 9;

    private static int CountCompleted(TenantOnboardingProgress p) =>
        (p.BusinessProfileCompleted ? 1 : 0) +
        (p.WorkingHoursCompleted ? 1 : 0) +
        (p.ServicesAdded ? 1 : 0) +
        (p.StaffAdded ? 1 : 0) +
        (p.BookingPageCustomized ? 1 : 0) +
        (p.PaymentSetupCompleted ? 1 : 0) +
        (p.FirstBookingCreated ? 1 : 0) +
        (p.ClientsImported ? 1 : 0);

    private static string GetNextStepHint(TenantOnboardingProgress p)
    {
        if (!p.BusinessProfileCompleted) return "Complete your business profile";
        if (!p.ServicesAdded) return "Add your first service";
        if (!p.StaffAdded) return "Add a staff member";
        if (!p.WorkingHoursCompleted) return "Set your working hours";
        if (!p.BookingPageCustomized) return "Customize your booking page";
        if (!p.PaymentSetupCompleted) return "Connect Stripe to accept payments";
        if (!p.FirstBookingCreated) return "Create your first test booking";
        return "Import your existing clients";
    }
}
