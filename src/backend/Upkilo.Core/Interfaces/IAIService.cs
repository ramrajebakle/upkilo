namespace Upkilo.Core.Interfaces;

public interface IAIService
{
    /// <summary>
    /// Generate text. Pass model = null to auto-resolve from the tenant's subscription tier (recommended).
    /// </summary>
    Task<AIGenerationResult> GenerateTextAsync(Guid tenantId, Guid? userId, string prompt, string? model = null);
    IAsyncEnumerable<string> GenerateTextStreamAsync(Guid tenantId, Guid? userId, string prompt, string? model = null);
    Task<AIGenerationResult> GenerateImageAsync(Guid tenantId, Guid? userId, string prompt);
    Task<AIGenerationResult> AnalyzeSentimentAsync(Guid tenantId, Guid? userId, string content);
    Task<AIGenerationResult> GenerateDiscoveryReportAsync(Guid tenantId, string businessType, string niche);
    Task<AIUsageStats> GetUsageStatsAsync(Guid tenantId, DateTime? from = null, DateTime? to = null);
    Task<bool> CheckQuotaAsync(Guid tenantId);
    Task<bool> CheckSafetyAsync(string content);
}

public class AIGenerationResult
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
    public string? Error { get; set; }
    /// <summary>Confidence score 0-100 estimated from output characteristics</summary>
    public double ConfidenceScore { get; set; }
    /// <summary>Whether this result should be held for human review</summary>
    public bool RequiresApproval { get; set; }
}

public class AIUsageStats
{
    public int TotalRequests { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public decimal TotalCost { get; set; }
    public Dictionary<string, int> RequestsByFeature { get; set; } = new();
    public Dictionary<string, decimal> CostByModel { get; set; } = new();
}
