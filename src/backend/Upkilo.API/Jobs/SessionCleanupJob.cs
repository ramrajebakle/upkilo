using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire job that cleans up expired and revoked user sessions.
/// Runs hourly to prevent session table bloat and enforce server-side
/// session expiry — critical for security compliance.
/// Also cleans up expired idempotency records (24h TTL).
/// </summary>
public class SessionCleanupJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionCleanupJob> _logger;

    public SessionCleanupJob(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("SessionCleanupJob started");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // 1. Clean expired sessions
        var expiredSessions = await context.UserSessions
            .Where(s => s.ExpiresAt < now || s.IsRevoked)
            .ToListAsync();

        if (expiredSessions.Count > 0)
        {
            context.UserSessions.RemoveRange(expiredSessions);
            _logger.LogInformation("Removed {Count} expired/revoked sessions", expiredSessions.Count);
        }

        // 2. Auto-revoke sessions inactive for > 24 hours
        var inactiveThreshold = now.AddHours(-24);
        var staleSessions = await context.UserSessions
            .Where(s => !s.IsRevoked && s.LastActiveAt < inactiveThreshold)
            .ToListAsync();

        foreach (var session in staleSessions)
        {
            session.IsRevoked = true;
        }

        if (staleSessions.Count > 0)
        {
            _logger.LogInformation("Auto-revoked {Count} inactive sessions (>24h)", staleSessions.Count);
        }

        // 3. Clean expired idempotency records
        var expiredKeys = await context.IdempotencyRecords
            .Where(r => r.ExpiresAt < now)
            .ToListAsync();

        if (expiredKeys.Count > 0)
        {
            context.IdempotencyRecords.RemoveRange(expiredKeys);
            _logger.LogInformation("Removed {Count} expired idempotency records", expiredKeys.Count);
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("SessionCleanupJob complete: {Expired} expired, {Stale} revoked, {Keys} keys cleaned",
            expiredSessions.Count, staleSessions.Count, expiredKeys.Count);
    }
}
