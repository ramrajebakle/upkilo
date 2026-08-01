using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Upkilo.Infrastructure.Data;
using Hangfire;
using Hangfire.Storage.Monitoring;

namespace Upkilo.API.HealthChecks;

/// <summary>
/// Verifies PostgreSQL database connectivity and responsiveness.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public DatabaseHealthCheck(AppDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Database.SetCommandTimeout(5);
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is responsive");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable", ex);
        }
    }
}

/// <summary>
/// Verifies Redis connectivity for caching and session state.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    public RedisHealthCheck(IConfiguration config) => _config = config;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var redisConnection = _config.GetConnectionString("Redis") ?? "localhost:6379";
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Use StackExchange.Redis to send a real PING command — TCP connect alone does
            // not verify Redis is ready (it may be loading RDB or in AOF rewrite).
            var opts = ConfigurationOptions.Parse(redisConnection, ignoreUnknown: true);
            opts.ConnectTimeout = 2000;
            opts.SyncTimeout = 2000;
            opts.AbortOnConnectFail = false;
            using var conn = await ConnectionMultiplexer.ConnectAsync(opts);
            var pong = await conn.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy($"Redis responsive — {pong.TotalMilliseconds:F0}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Redis is unreachable (caching disabled)", ex);
        }
    }
}

/// <summary>
/// Reports application memory usage and uptime.
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var memoryMb = process.WorkingSet64 / 1024 / 1024;
        var uptime = DateTime.UtcNow - StartTime;

        var data = new Dictionary<string, object>
        {
            ["memory_mb"] = memoryMb,
            ["uptime_seconds"] = (long)uptime.TotalSeconds,
            ["uptime_display"] = uptime.ToString(@"d\.hh\:mm\:ss"),
            ["threads"] = process.Threads.Count,
            ["gc_gen0"] = GC.CollectionCount(0),
            ["gc_gen1"] = GC.CollectionCount(1),
            ["gc_gen2"] = GC.CollectionCount(2)
        };

        // Warn if memory exceeds 512MB
        if (memoryMb > 512)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"High memory usage: {memoryMb}MB", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"OK — {memoryMb}MB, uptime {uptime:d\\.hh\\:mm\\:ss}", data: data));
    }
}

/// <summary>
/// Verifies Hangfire background job processor is operational and not overloaded.
/// A deep queue backlog indicates billing/reminder jobs may be delayed.
/// </summary>
public class HangfireHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = JobStorage.Current.GetMonitoringApi();
            var stats = api.GetStatistics();

            var data = new Dictionary<string, object>
            {
                ["enqueued"] = stats.Enqueued,
                ["processing"] = stats.Processing,
                ["failed"] = stats.Failed,
                ["scheduled"] = stats.Scheduled,
                ["succeeded"] = stats.Succeeded
            };

            if (stats.Enqueued > 5000)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Hangfire queue critically overloaded: {stats.Enqueued} jobs", data: data));
            }

            if (stats.Enqueued > 1000 || stats.Failed > 50)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Hangfire degraded — enqueued: {stats.Enqueued}, failed: {stats.Failed}", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Hangfire healthy — enqueued: {stats.Enqueued}, failed: {stats.Failed}", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Hangfire storage unreachable — background jobs suspended", ex));
        }
    }
}
