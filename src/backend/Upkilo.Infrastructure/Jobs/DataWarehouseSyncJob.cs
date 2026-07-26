using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Hangfire job that drives incremental data warehouse sync every 15 minutes.
/// Scheduled via RecurringJob.AddOrUpdate in Program.cs or at startup.
/// </summary>
public class DataWarehouseSyncJob
{
    private readonly DataWarehouseSyncService _syncService;
    private readonly ILogger<DataWarehouseSyncJob> _logger;

    public DataWarehouseSyncJob(DataWarehouseSyncService syncService, ILogger<DataWarehouseSyncJob> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task RunAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DataWarehouseSyncJob started for tenant {TenantId}", tenantId);

        var tables = new[] { "bookings", "clients", "invoices" };
        foreach (var table in tables)
        {
            try
            {
                using var tableCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                tableCts.CancelAfter(TimeSpan.FromMinutes(5));
                await _syncService.RunIncrementalSyncAsync(tenantId, table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DataWarehouseSyncJob failed for tenant {TenantId} table {Table}. Continuing with remaining tables.", tenantId, table);
            }
        }

        _logger.LogInformation("DataWarehouseSyncJob completed for tenant {TenantId}", tenantId);
    }
}
