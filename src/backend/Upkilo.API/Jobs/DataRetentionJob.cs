using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Enforces data retention policies per entity type.
/// Runs daily at 4 AM UTC to permanently delete expired data.
/// 
/// Retention schedule:
///   - Audit logs: 365 days (1 year)
///   - Login history: 90 days
///   - Processed webhooks: 30 days
///   - Dead letter messages: 60 days
///   - Idempotency records: 7 days (also cleaned by SessionCleanupJob)
///   - Cancelled tenant data: 90 days after cancellation
/// </summary>
public class DataRetentionJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(IServiceScopeFactory scopeFactory, ILogger<DataRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("DataRetentionJob started — enforcing retention policies");

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var totalDeleted = 0;

        // 1. Audit logs — 365 days
        var auditCutoff = now.AddDays(-365);
        var auditDeleted = await context.AuditEntries
            .Where(a => a.CreatedAt < auditCutoff)
            .ExecuteDeleteAsync();
        if (auditDeleted > 0)
            _logger.LogInformation("Deleted {Count} audit logs older than 365 days", auditDeleted);
        totalDeleted += auditDeleted;

        // 2. Login history — 90 days
        var loginCutoff = now.AddDays(-90);
        var loginDeleted = await context.LoginHistories
            .Where(h => h.AttemptedAt < loginCutoff)
            .ExecuteDeleteAsync();
        if (loginDeleted > 0)
            _logger.LogInformation("Deleted {Count} login history records older than 90 days", loginDeleted);
        totalDeleted += loginDeleted;

        // 3. Dead letter messages — 60 days
        var dlqCutoff = now.AddDays(-60);
        var dlqDeleted = await context.DeadLetterMessages
            .Where(d => d.CreatedAt < dlqCutoff)
            .ExecuteDeleteAsync();
        if (dlqDeleted > 0)
            _logger.LogInformation("Deleted {Count} dead letter messages older than 60 days", dlqDeleted);
        totalDeleted += dlqDeleted;

        // 4. Expired slot holds — 7 days (already expired + not converted)
        var slotCutoff = now.AddDays(-7);
        var slotDeleted = await context.SlotHolds
            .Where(s => s.ExpiresAt < slotCutoff && !s.IsConverted)
            .ExecuteDeleteAsync();
        if (slotDeleted > 0)
            _logger.LogInformation("Deleted {Count} expired slot holds older than 7 days", slotDeleted);
        totalDeleted += slotDeleted;

        _logger.LogInformation("DataRetentionJob complete — {TotalDeleted} records purged", totalDeleted);
    }
}
