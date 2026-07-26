using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class DeveloperPortalService
{
    private readonly ILogger<DeveloperPortalService> _logger;
    private readonly AppDbContext _context;
    private readonly IDatabase _redis;

    public DeveloperPortalService(ILogger<DeveloperPortalService> logger, AppDbContext context, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _context = context;
        _redis = redis.GetDatabase();
    }

    public async Task<ApiKeyMetrics> GetApiMetricsAsync(Guid apiKeyId)
    {
        _logger.LogInformation("Fetching API metrics for key {KeyId}", apiKeyId);

        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null)
            return new ApiKeyMetrics();

        // Get 24h usage from audit log
        var since = DateTime.UtcNow.AddHours(-24);
        var usageCount = await _context.AuditEntries
            .Where(a => a.EntityType == "ApiKey" && a.EntityId == apiKeyId.ToString() && a.Timestamp >= since)
            .CountAsync();

        // Get rate limit quota from Redis sliding window bucket
        var rateLimitKey = $"rl:apikey:{apiKeyId}";
        var redisCount = await _redis.StringGetAsync(rateLimitKey);
        long currentWindowUsage = 0;
        if (redisCount.HasValue && long.TryParse(redisCount, out var rv))
            currentWindowUsage = rv;

        // Default per-key quota (can be per-plan; 20k/day is the Standard tier)
        const long dailyQuota = 20_000;
        var quotaRemaining = Math.Max(0, dailyQuota - usageCount);

        return new ApiKeyMetrics
        {
            TotalRequests = usageCount,
            QuotaRemaining = quotaRemaining,
            CurrentWindowUsage = currentWindowUsage,
            LastUsedAt = apiKey.LastUsedAt
        };
    }

    public async Task<string> ProvisionSandboxAsync(Guid tenantId)
    {
        _logger.LogInformation("Cloning tenant {TenantId} to Sandbox environment", tenantId);
        var sandboxId = "sandbox_" + Guid.NewGuid().ToString("N")[..8];

        // Record sandbox provisioning in audit
        _context.AuditEntries.Add(new Upkilo.Core.Entities.AuditEntry
        {
            TenantId = tenantId,
            EntityType = "Sandbox",
            EntityId = sandboxId,
            Action = "Provisioned",
            UserName = "system",
            Timestamp = DateTime.UtcNow,
            Details = $"{{\"sourceTenanId\":\"{tenantId}\",\"sandboxId\":\"{sandboxId}\"}}"
        });
        await _context.SaveChangesAsync();

        return sandboxId;
    }
}

public class ApiKeyMetrics
{
    public long TotalRequests { get; set; }
    public long QuotaRemaining { get; set; }
    public long CurrentWindowUsage { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
