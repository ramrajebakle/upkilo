using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Tracks real API key usage metrics (replaces mock data)
/// </summary>
public class ApiKeyUsageService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApiKeyUsageService> _logger;

    public ApiKeyUsageService(AppDbContext context, ILogger<ApiKeyUsageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Records an API key usage event
    /// </summary>
    public async Task RecordUsageAsync(Guid apiKeyId, string endpoint, int statusCode, long responseTimeMs)
    {
        var key = await _context.ApiKeys.FindAsync(apiKeyId);
        if (key == null) return;

        key.LastUsedAt = DateTime.UtcNow;

        // Log to audit
        _context.AuditEntries.Add(new AuditEntry
        {
            TenantId = key.TenantId,
            EntityType = "ApiKey",
            EntityId = apiKeyId.ToString(),
            Action = "ApiCall",
            UserName = $"apikey:{key.Name}",
            Timestamp = DateTime.UtcNow,
            Details = $"{{\"endpoint\":\"{endpoint}\",\"status\":{statusCode},\"responseMs\":{responseTimeMs}}}"
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets usage statistics for an API key
    /// </summary>
    public async Task<ApiKeyUsageStats> GetUsageStatsAsync(Guid apiKeyId, DateTime from, DateTime to)
    {
        var key = await _context.ApiKeys.FindAsync(apiKeyId);
        if (key == null) return new ApiKeyUsageStats();

        var usageLogs = await _context.AuditEntries
            .Where(a => a.EntityType == "ApiKey" && a.EntityId == apiKeyId.ToString() && a.Timestamp >= from && a.Timestamp <= to)
            .ToListAsync();

        return new ApiKeyUsageStats
        {
            ApiKeyId = apiKeyId,
            TotalRequests = usageLogs.Count,
            LastUsedAt = key.LastUsedAt,
            CreatedAt = key.CreatedAt,
            PeriodStart = from,
            PeriodEnd = to,
            DailyBreakdown = usageLogs
                .GroupBy(l => l.Timestamp.Date)
                .Select(g => new DailyUsage { Date = g.Key, Count = g.Count() })
                .OrderBy(d => d.Date)
                .ToList()
        };
    }
}

public class ApiKeyUsageStats
{
    public Guid ApiKeyId { get; set; }
    public int TotalRequests { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<DailyUsage> DailyBreakdown { get; set; } = new();
}

public class DailyUsage
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
