namespace Upkilo.Core.Interfaces;

public interface IAutomationSafetyService
{
    Task<SafetyCheckResult> EvaluateCampaignHealthAsync(Guid tenantId, Guid? campaignId = null);
    Task<int> EnforceSafetyActionsAsync(Guid tenantId, SafetyCheckResult checkResult);
}

public class SafetyCheckResult
{
    public Guid TenantId { get; set; }
    public Guid? CampaignId { get; set; }
    public AutomationRiskLevel OverallRisk { get; set; }
    public List<SafetyAction> Actions { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

public class SafetyAction
{
    public SafetyActionType ActionType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AutomationRiskLevel RiskLevel { get; set; }
    public string AffectedResource { get; set; } = string.Empty;
}

public enum SafetyActionType
{
    AutoPause,
    Rollback,
    QueueForReview,
    Alert
}

public enum AutomationRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
