using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Background job to synchronize Google/Outlook calendars for all active tenants.
/// </summary>
public class CalendarSyncJob
{
    private readonly AppDbContext _context;
    private readonly IEnumerable<ICalendarService> _calendarServices;
    private readonly ILogger<CalendarSyncJob> _logger;

    public CalendarSyncJob(
        AppDbContext context, 
        IEnumerable<ICalendarService> calendarServices,
        ILogger<CalendarSyncJob> logger)
    {
        _context = context;
        _calendarServices = calendarServices;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting global calendar synchronization job...");

        var tenantsToSync = await _context.Tenants
            .Where(t => t.Status == Upkilo.Core.Entities.TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        // Process tenants in parallel with a concurrency cap to avoid overwhelming calendar APIs.
        await Parallel.ForEachAsync(tenantsToSync,
            new ParallelOptions { MaxDegreeOfParallelism = 20, CancellationToken = cancellationToken },
            async (tenantId, _) =>
            {
                try
                {
                    _logger.LogDebug("Syncing calendars for tenant: {TenantId}", tenantId);

                    foreach (var service in _calendarServices)
                    {
                        using var perTenantCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        await service.SyncBookingsAsync(tenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync calendars for tenant: {TenantId}", tenantId);
                }
            });

        _logger.LogInformation("Global calendar synchronization job completed.");
    }
}
