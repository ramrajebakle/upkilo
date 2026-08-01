using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Entities;

namespace Upkilo.API.Jobs;

/// <summary>
/// Background job to purge old audit logs based on tenant retention policies
/// </summary>
public class AuditLogRetentionJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogRetentionJob> _logger;

    public AuditLogRetentionJob(AppDbContext context, ILogger<AuditLogRetentionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting Audit Log Retention Job");

        // Process each tenant's retention policy
        var subscriptions = await _context.Set<Subscription>()
            .AsNoTracking()
            .ToListAsync();

        int totalDeleted = 0;
        const int DefaultRetentionDays = 90; // Fallback for tenants without specific plans
        var tenantsWithSubs = subscriptions.Select(s => s.TenantId).ToHashSet();

        // 1. Process explicit retention from subscriptions
        foreach (var sub in subscriptions)
        {
            var retentionDays = sub.AuditLogRetentionDays > 0 ? sub.AuditLogRetentionDays : DefaultRetentionDays;
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var deletedCount = await _context.AuditEntries
                .Where(l => l.TenantId == sub.TenantId && l.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} old audit logs for tenant {TenantId} (Retention: {Days} days)",
                    deletedCount, sub.TenantId, retentionDays);
                totalDeleted += deletedCount;
            }
        }

        // 2. Process tenants without explicit subscriptions (Global Default)
        var orphanTenants = await _context.Tenants
            .Where(t => !tenantsWithSubs.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var tenantId in orphanTenants)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-DefaultRetentionDays);
            var deletedCount = await _context.AuditEntries
                .Where(l => l.TenantId == tenantId && l.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} old audit logs for orphan tenant {TenantId} (Default: {Days} days)",
                    deletedCount, tenantId, DefaultRetentionDays);
                totalDeleted += deletedCount;
            }
        }

        _logger.LogInformation("Audit Log Retention Job completed. Total logs deleted: {Total}", totalDeleted);
    }
}
