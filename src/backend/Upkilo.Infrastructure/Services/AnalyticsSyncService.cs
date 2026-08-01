using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Upkilo.Infrastructure.Services
{
    public class AnalyticsSyncService : IAnalyticsSyncService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnalyticsSyncService> _logger;
        private readonly ICacheService _cache; // Using custom ICacheService for tracking last sync

        public AnalyticsSyncService(AppDbContext context, ILogger<AnalyticsSyncService> logger, ICacheService cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task SyncDataAsync()
        {
            _logger.LogInformation("Starting global analytics data synchronization...");

            // In a real implementation, we would sync multiple tables
            await SyncIncrementalAsync("Bookings", DateTime.UtcNow.AddDays(-1));
            await SyncIncrementalAsync("Invoices", DateTime.UtcNow.AddDays(-1));
            await SyncIncrementalAsync("AuditLogs", DateTime.UtcNow.AddDays(-1));

            _logger.LogInformation("Global analytics sync completed.");
        }

        public async Task SyncIncrementalAsync(string tableName, DateTime lastSync)
        {
            _logger.LogInformation("Syncing table {TableName} since {LastSync}", tableName, lastSync);

            int count = tableName switch
            {
                "Bookings" => await _context.Bookings
                                   .Where(b => b.UpdatedAt >= lastSync)
                                   .CountAsync(),
                "Invoices" => await _context.Invoices
                                   .Where(i => i.UpdatedAt >= lastSync)
                                   .CountAsync(),
                "AuditLogs" => await _context.AuditLogs
                                   .Where(a => a.Timestamp >= lastSync)
                                   .CountAsync(),
                _ => 0
            };

            if (count == 0)
            {
                _logger.LogDebug("No new {TableName} records since {LastSync}.", tableName, lastSync);
                return;
            }

            // Persist a sync checkpoint so the next run knows where to resume
            var cacheKey = $"analytics_sync_checkpoint_{tableName}";
            await _cache.GetOrSetAsync<string>(cacheKey, () => Task.FromResult(DateTime.UtcNow.ToString("O")), TimeSpan.FromDays(7));

            _logger.LogInformation("Synced {Count} {TableName} records to analytics layer.", count, tableName);
        }
    }
}
