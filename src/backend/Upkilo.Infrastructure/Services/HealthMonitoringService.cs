using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class HealthMonitoringService
{
    private readonly ILogger<HealthMonitoringService> _logger;
    private readonly AppDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly PagerDutyService _pagerDuty;

    public HealthMonitoringService(
        ILogger<HealthMonitoringService> logger,
        AppDbContext context,
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        PagerDutyService pagerDuty)
    {
        _logger = logger;
        _context = context;
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _pagerDuty = pagerDuty;
    }

    public async Task<HealthCheckReport> CheckAllHealthAsync()
    {
        _logger.LogInformation("Running multi-system health check...");
        var report = new HealthCheckReport { CheckedAt = DateTime.UtcNow };

        await Task.WhenAll(
            CheckDatabaseAsync(report),
            CheckRedisAsync(report),
            CheckStripeAsync(report),
            CheckHangfireAsync(report)
        );

        report.IsHealthy = report.Checks.All(c => c.Status == "Healthy");
        _logger.LogInformation("Health check complete. Healthy: {Healthy}", report.IsHealthy);

        // Alert PagerDuty for any unhealthy check
        foreach (var check in report.Checks)
        {
            if (check.Status == "Unhealthy")
            {
                await _pagerDuty.TriggerAlertAsync(
                    summary: $"Health check Unhealthy: {check.Name}",
                    severity: "critical",
                    source: $"HealthMonitoringService/{check.Name}",
                    details: new { check.Name, check.Status, check.Details });
            }
        }

        return report;
    }

    private async Task CheckDatabaseAsync(HealthCheckReport report)
    {
        var check = new HealthCheck { Name = "Database" };
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            check.Status = "Healthy";
            check.Details = "PostgreSQL connection OK";
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Details = ex.Message;
            _logger.LogError(ex, "Database health check failed");
        }
        report.Checks.Add(check);
    }

    private async Task CheckRedisAsync(HealthCheckReport report)
    {
        var check = new HealthCheck { Name = "Redis" };
        try
        {
            var db = _redis.GetDatabase();
            var pong = await db.PingAsync();
            check.Status = "Healthy";
            check.Details = $"Redis latency: {pong.TotalMilliseconds:F1}ms";
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Details = ex.Message;
            _logger.LogError(ex, "Redis health check failed");
        }
        report.Checks.Add(check);
    }

    private async Task CheckStripeAsync(HealthCheckReport report)
    {
        var check = new HealthCheck { Name = "Stripe" };
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync("https://status.stripe.com/api/v2/status.json");
            if (response.IsSuccessStatusCode)
            {
                check.Status = "Healthy";
                check.Details = "Stripe API reachable";
            }
            else
            {
                check.Status = "Degraded";
                check.Details = $"Stripe status: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Details = ex.Message;
            _logger.LogWarning(ex, "Stripe reachability check failed");
        }
        report.Checks.Add(check);
    }

    private async Task CheckHangfireAsync(HealthCheckReport report)
    {
        var check = new HealthCheck { Name = "Hangfire" };
        try
        {
            // Check for stuck jobs: any job in Processing state older than 30 minutes indicates a dead worker
            var stuckThreshold = DateTime.UtcNow.AddMinutes(-30);
            var stuckJobCount = await _context.Set<Upkilo.Core.Entities.OutboxMessage>()
                .Where(m => !m.IsProcessed && m.CreatedAt < stuckThreshold)
                .CountAsync();

            if (stuckJobCount > 50)
            {
                check.Status = "Degraded";
                check.Details = $"{stuckJobCount} unprocessed outbox messages older than 30 min";
            }
            else
            {
                check.Status = "Healthy";
                check.Details = $"Outbox backlog: {stuckJobCount} pending";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unknown";
            check.Details = ex.Message;
            _logger.LogWarning(ex, "Hangfire health check failed");
        }
        report.Checks.Add(check);
    }
}

public class HealthCheckReport
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<HealthCheck> Checks { get; set; } = new();
}

public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown"; // Healthy, Degraded, Unhealthy, Unknown
    public string? Details { get; set; }
}
