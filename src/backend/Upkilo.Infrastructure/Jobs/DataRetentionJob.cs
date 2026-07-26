using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Data Retention Job — enforces configurable retention policies by purging
/// records older than their retention window.
///
/// Default policies:
///   AuditLogs     → 365 days
///   OutboxMessages → 90 days (processed only)
///   LoginHistory  → 180 days
///   Notifications → 60 days
///
/// Scheduled via Hangfire (registered in Program.cs):
///   RecurringJob.AddOrUpdate&lt;DataRetentionJob&gt;(
///       "data-retention", j =&gt; j.RunAsync(CancellationToken.None), Cron.Daily(2));
/// </summary>
public class DataRetentionJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataRetentionJob> _logger;

    // Retention settings (can be overridden via appsettings.json)
    private readonly RetentionPolicy _policy;

    public DataRetentionJob(
        AppDbContext db,
        ILogger<DataRetentionJob> logger,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;

        var section = configuration.GetSection("DataRetention");
        _policy = new RetentionPolicy
        {
            AuditLogDays = section.GetValue<int?>("AuditLogDays") ?? 365,
            OutboxMessageDays = section.GetValue<int?>("OutboxMessageDays") ?? 90,
            LoginHistoryDays = section.GetValue<int?>("LoginHistoryDays") ?? 180,
            NotificationDays = section.GetValue<int?>("NotificationDays") ?? 60,
            BatchSize = section.GetValue<int?>("BatchSize") ?? 500
        };
    }

    // ── Entry point (called by Hangfire) ──────────────────────────────────────

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DataRetentionJob started at {Time}", DateTime.UtcNow);
        var summary = new RetentionSummary { StartedAt = DateTime.UtcNow };

        try
        {
            summary.AuditLogsDeleted = await PurgeAuditLogsAsync(cancellationToken);
            summary.OutboxMessagesDeleted = await PurgeProcessedOutboxMessagesAsync(cancellationToken);
            summary.LoginHistoryDeleted = await PurgeLoginHistoryAsync(cancellationToken);
            summary.NotificationsDeleted = await PurgeNotificationsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataRetentionJob encountered an error");
            summary.Error = ex.Message;
        }
        finally
        {
            summary.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "DataRetentionJob completed: AuditLogs={AL} Outbox={OB} LoginHistory={LH} Notifications={NT} Duration={Ms}ms",
                summary.AuditLogsDeleted,
                summary.OutboxMessagesDeleted,
                summary.LoginHistoryDeleted,
                summary.NotificationsDeleted,
                summary.DurationMs);
        }
    }

    // ── Per-table purge methods ────────────────────────────────────────────────

    private async Task<int> PurgeAuditLogsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_policy.AuditLogDays);
        int totalDeleted = 0;

        // Table: AuditLogs (check if entity exists via reflection-safe check)
        try
        {
            int deleted;
            do
            {
                deleted = await _db.Database.ExecuteSqlAsync(
                    $"DELETE FROM \"AuditLogs\" WHERE \"CreatedAt\" < {cutoff} AND ctid IN (SELECT ctid FROM \"AuditLogs\" WHERE \"CreatedAt\" < {cutoff} LIMIT {_policy.BatchSize})",
                    ct);
                totalDeleted += deleted;
            } while (deleted == _policy.BatchSize && !ct.IsCancellationRequested);

            if (totalDeleted > 0)
                _logger.LogInformation("Purged {Count} audit log entries older than {Days} days", totalDeleted, _policy.AuditLogDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AuditLog purge failed — table may not exist or use different name");
        }

        return totalDeleted;
    }

    private async Task<int> PurgeProcessedOutboxMessagesAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_policy.OutboxMessageDays);
        int totalDeleted = 0;

        try
        {
            // Only purge messages that have been successfully processed
            int deleted;
            do
            {
                var batch = await _db.OutboxMessages
                    .Where(m => m.ProcessedAt != null && m.CreatedAt < cutoff)
                    .OrderBy(m => m.CreatedAt)
                    .Take(_policy.BatchSize)
                    .ToListAsync(ct);

                if (batch.Count == 0) break;

                _db.OutboxMessages.RemoveRange(batch);
                await _db.SaveChangesAsync(ct);

                deleted = batch.Count;
                totalDeleted += deleted;
            } while (totalDeleted < 100_000 && !ct.IsCancellationRequested);

            if (totalDeleted > 0)
                _logger.LogInformation("Purged {Count} processed outbox messages older than {Days} days", totalDeleted, _policy.OutboxMessageDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Outbox message purge failed");
        }

        return totalDeleted;
    }

    private async Task<int> PurgeLoginHistoryAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_policy.LoginHistoryDays);
        int totalDeleted = 0;

        try
        {
            int deleted;
            do
            {
                deleted = await _db.Database.ExecuteSqlAsync(
                    $"DELETE FROM \"LoginHistory\" WHERE \"LoginAt\" < {cutoff} AND ctid IN (SELECT ctid FROM \"LoginHistory\" WHERE \"LoginAt\" < {cutoff} LIMIT {_policy.BatchSize})",
                    ct);
                totalDeleted += deleted;
            } while (deleted == _policy.BatchSize && !ct.IsCancellationRequested);

            if (totalDeleted > 0)
                _logger.LogInformation("Purged {Count} login history entries older than {Days} days", totalDeleted, _policy.LoginHistoryDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoginHistory purge failed");
        }

        return totalDeleted;
    }

    private async Task<int> PurgeNotificationsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_policy.NotificationDays);
        int totalDeleted = 0;

        try
        {
            int deleted;
            do
            {
                deleted = await _db.Database.ExecuteSqlAsync(
                    $"DELETE FROM \"Notifications\" WHERE \"CreatedAt\" < {cutoff} AND \"IsRead\" = true AND ctid IN (SELECT ctid FROM \"Notifications\" WHERE \"CreatedAt\" < {cutoff} AND \"IsRead\" = true LIMIT {_policy.BatchSize})",
                    ct);
                totalDeleted += deleted;
            } while (deleted == _policy.BatchSize && !ct.IsCancellationRequested);

            if (totalDeleted > 0)
                _logger.LogInformation("Purged {Count} read notifications older than {Days} days", totalDeleted, _policy.NotificationDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notifications purge failed");
        }

        return totalDeleted;
    }
}

// ─── Supporting types ─────────────────────────────────────────────────────────

public class RetentionPolicy
{
    public int AuditLogDays { get; set; } = 365;
    public int OutboxMessageDays { get; set; } = 90;
    public int LoginHistoryDays { get; set; } = 180;
    public int NotificationDays { get; set; } = 60;
    public int BatchSize { get; set; } = 500;
}

public class RetentionSummary
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int AuditLogsDeleted { get; set; }
    public int OutboxMessagesDeleted { get; set; }
    public int LoginHistoryDeleted { get; set; }
    public int NotificationsDeleted { get; set; }
    public string? Error { get; set; }
    public double DurationMs => (CompletedAt - StartedAt).TotalMilliseconds;
    public int TotalDeleted => AuditLogsDeleted + OutboxMessagesDeleted + LoginHistoryDeleted + NotificationsDeleted;
}
