using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class OperationalDashboardService
{
    private readonly AppDbContext _context;
    private readonly IDatabase _redis;

    public OperationalDashboardService(AppDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis.GetDatabase();
    }

    public async Task<List<DashboardMetric>> GetSystemHealthDashboardsAsync()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(-1);

        var tasks = await Task.WhenAll(
            GetDatabaseMetricAsync(),
            GetRedisCacheHitRateAsync(),
            GetJobSuccessRateAsync(windowStart, now),
            GetActiveTenantsMetricAsync(),
            GetPendingOutboxMetricAsync()
        );

        return tasks.ToList();
    }

    private async Task<DashboardMetric> GetDatabaseMetricAsync()
    {
        try
        {
            var slowQueryCount = await _context.AuditEntries
                .Where(a => a.Action == "SlowQuery" && a.Timestamp > DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            return new DashboardMetric
            {
                Name = "Database Slow Queries (1h)",
                Value = slowQueryCount,
                Status = slowQueryCount < 10 ? "Green" : slowQueryCount < 50 ? "Yellow" : "Red"
            };
        }
        catch
        {
            return new DashboardMetric { Name = "Database Slow Queries (1h)", Value = -1, Status = "Unknown" };
        }
    }

    private async Task<DashboardMetric> GetRedisCacheHitRateAsync()
    {
        try
        {
            var server = _redis.Multiplexer.GetServers().FirstOrDefault();
            if (server == null)
                return new DashboardMetric { Name = "Redis Cache Hit Rate", Value = 0, Status = "Unknown" };

            var info = await server.InfoAsync("stats");
            long hits = 0, misses = 0;
            foreach (var group in info)
            {
                foreach (var item in group)
                {
                    if (item.Key == "keyspace_hits" && long.TryParse(item.Value, out var h)) hits = h;
                    if (item.Key == "keyspace_misses" && long.TryParse(item.Value, out var m)) misses = m;
                }
            }

            var total = hits + misses;
            var hitRate = total > 0 ? Math.Round((double)hits / total * 100, 1) : 0;
            return new DashboardMetric
            {
                Name = "Redis Cache Hit Rate (%)",
                Value = hitRate,
                Status = hitRate >= 80 ? "Green" : hitRate >= 60 ? "Yellow" : "Red"
            };
        }
        catch
        {
            return new DashboardMetric { Name = "Redis Cache Hit Rate (%)", Value = 0, Status = "Unknown" };
        }
    }

    private async Task<DashboardMetric> GetJobSuccessRateAsync(DateTime from, DateTime to)
    {
        try
        {
            var processed = await _context.OutboxMessages
                .Where(m => m.ProcessedAt >= from && m.ProcessedAt <= to)
                .CountAsync();

            var failed = await _context.OutboxMessages
                .Where(m => m.RetryCount >= 3 && m.CreatedAt >= from && m.CreatedAt <= to && !m.IsProcessed)
                .CountAsync();

            var total = processed + failed;
            var successRate = total > 0 ? Math.Round((double)processed / total * 100, 1) : 100.0;
            return new DashboardMetric
            {
                Name = "Job Success Rate (1h, %)",
                Value = successRate,
                Status = successRate >= 95 ? "Green" : successRate >= 80 ? "Yellow" : "Red"
            };
        }
        catch
        {
            return new DashboardMetric { Name = "Job Success Rate (1h, %)", Value = 0, Status = "Unknown" };
        }
    }

    private async Task<DashboardMetric> GetActiveTenantsMetricAsync()
    {
        try
        {
            var activeTenants = await _context.Tenants
                .Where(t => t.IsActive)
                .CountAsync();

            return new DashboardMetric
            {
                Name = "Active Tenants",
                Value = activeTenants,
                Status = "Green"
            };
        }
        catch
        {
            return new DashboardMetric { Name = "Active Tenants", Value = 0, Status = "Unknown" };
        }
    }

    private async Task<DashboardMetric> GetPendingOutboxMetricAsync()
    {
        try
        {
            var pending = await _context.OutboxMessages
                .Where(m => !m.IsProcessed)
                .CountAsync();

            return new DashboardMetric
            {
                Name = "Pending Outbox Messages",
                Value = pending,
                Status = pending < 100 ? "Green" : pending < 500 ? "Yellow" : "Red"
            };
        }
        catch
        {
            return new DashboardMetric { Name = "Pending Outbox Messages", Value = 0, Status = "Unknown" };
        }
    }
}

public class DashboardMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Status { get; set; } = "Green";
}
