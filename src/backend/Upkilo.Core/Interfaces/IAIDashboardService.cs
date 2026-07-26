using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IAIDashboardService
{
    Task<IEnumerable<AIDecisionLogDto>> GetDecisionLogsAsync(Guid tenantId, int count = 20);
    Task<AIDashboardMetricsDto> GetDashboardMetricsAsync(Guid tenantId);
    Task<IEnumerable<AITokenUsageDto>> GetTokenUsageTrendsAsync(Guid tenantId, int days = 30);
    Task<bool> ApproveDecisionAsync(Guid tenantId, Guid decisionId, Guid userId);
    Task LogDecisionAsync(Guid tenantId, string agentName, string decisionType, string input, string output, decimal confidence, string model = "gpt-4", int inputTokens = 0, int outputTokens = 0, Guid? entityId = null, string? entityType = null);
}

public class AIDecisionLogDto
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string DecisionType { get; set; } = string.Empty;
    public string OutputDecision { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string Model { get; set; } = "gpt-4";
    public DateTime CreatedAt { get; set; }
    public bool RequiresHumanReview { get; set; }
    public bool IsApproved { get; set; }
}

public class AIDashboardMetricsDto
{
    public int TotalDecisions { get; set; }
    public int PendingReviews { get; set; }
    public decimal AvgConfidence { get; set; }
    public decimal TotalCost { get; set; }
    public int TotalTokens { get; set; }
    public Dictionary<string, int> DecisionsByAgent { get; set; } = new();
    public Dictionary<string, int> UsageByModel { get; set; } = new();
}

public class AITokenUsageDto
{
    public DateTime Date { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
}
