using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Enforces per-tenant background job quotas based on subscription tier.
/// Uses the real Hangfire IMonitoringApi to count active/scheduled jobs.
/// </summary>
public class JobQuotaService
{
    private readonly AppDbContext _context;

    public JobQuotaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanScheduleJobAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return false;

        var quota = GetQuotaForTier(tenant.SubscriptionTier);
        if (quota == int.MaxValue) return true;

        var api = JobStorage.Current.GetMonitoringApi();

        // Count jobs that are currently processing or enqueued for this tenant.
        // Hangfire doesn't natively filter by tenant, so we inspect job parameters.
        var processing   = api.ProcessingJobs(0, int.MaxValue);
        var enqueued     = api.EnqueuedJobs("default", 0, int.MaxValue);

        var tenantIdStr  = tenantId.ToString();

        var activeForTenant = processing.Count(j =>
            j.Value?.Job?.Args?.Any(a => a?.ToString() == tenantIdStr) == true);

        var enqueuedForTenant = enqueued.Count(j =>
            j.Value?.Job?.Args?.Any(a => a?.ToString() == tenantIdStr) == true);

        var total = activeForTenant + enqueuedForTenant;

        return total < quota;
    }

    private static int GetQuotaForTier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free         => 1,
        SubscriptionTier.Starter      => 5,
        SubscriptionTier.Professional => 20,
        SubscriptionTier.Business     => 100,
        SubscriptionTier.Enterprise   => int.MaxValue,
        _                             => 1
    };
}
