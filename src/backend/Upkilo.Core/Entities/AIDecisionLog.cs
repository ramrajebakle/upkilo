using System;

namespace Upkilo.Core.Entities;

public class AIDecisionLog : TenantEntity
{
    public string AgentName { get; set; } = string.Empty;
    public string DecisionType { get; set; } = string.Empty; // e.g., "ChurnPrediction", "GrowthRecommendation"
    public string InputData { get; set; } = string.Empty; // JSON or text context
    public string OutputDecision { get; set; } = string.Empty; // The actual recommendation/choice
    public decimal ConfidenceScore { get; set; }
    public string Model { get; set; } = "gpt-4";
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public Guid? RelatedEntityId { get; set; } // e.g., ClientId or BookingId
    public string? RelatedEntityType { get; set; }
    public bool RequiresHumanReview { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Feedback { get; set; }
}
