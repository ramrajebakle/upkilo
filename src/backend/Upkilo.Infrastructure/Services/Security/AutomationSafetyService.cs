using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Security;

/// <summary>
/// Monitors marketing automation health metrics and enforces safety guardrails.
/// Implements auto-pause on traffic drops, rollback on error rates, and risk classification.
/// </summary>
public class AutomationSafetyService : IAutomationSafetyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AutomationSafetyService> _logger;

    // Safety thresholds
    private const double TrafficDropPauseThreshold = 0.20;   // 20% drop triggers pause
    private const double ErrorRateRollbackThreshold = 0.05;  // 5% error rate triggers rollback
    private const double DuplicateContentThreshold = 0.85;   // 85% similarity triggers pause

    public AutomationSafetyService(
        AppDbContext context,
        ILogger<AutomationSafetyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates the health of a marketing automation campaign and returns safety status.
    /// Should be called periodically (e.g., every 15 minutes via Hangfire).
    /// </summary>
    public async Task<SafetyCheckResult> EvaluateCampaignHealthAsync(Guid tenantId, Guid? campaignId = null)
    {
        var result = new SafetyCheckResult { TenantId = tenantId, CampaignId = campaignId };

        // Check 1: Traffic drop detection
        var trafficCheck = await CheckTrafficDropAsync(tenantId);
        if (trafficCheck.ShouldPause)
        {
            result.Actions.Add(new SafetyAction
            {
                ActionType = SafetyActionType.AutoPause,
                Reason = $"Traffic dropped {trafficCheck.DropPercentage:P0} in last 24h (threshold: {TrafficDropPauseThreshold:P0})",
                RiskLevel = AutomationRiskLevel.High,
                AffectedResource = "Campaign traffic"
            });
        }

        // Check 2: Error rate monitoring
        var errorCheck = await CheckErrorRateAsync(tenantId, campaignId);
        if (errorCheck.ShouldRollback)
        {
            result.Actions.Add(new SafetyAction
            {
                ActionType = SafetyActionType.Rollback,
                Reason = $"Error rate at {errorCheck.ErrorRate:P1} (threshold: {ErrorRateRollbackThreshold:P1})",
                RiskLevel = AutomationRiskLevel.Critical,
                AffectedResource = "Workflow executions"
            });
        }

        // Check 3: Duplicate content detection
        var duplicateCheck = await CheckDuplicateContentAsync(tenantId);
        if (duplicateCheck.HasDuplicates)
        {
            result.Actions.Add(new SafetyAction
            {
                ActionType = SafetyActionType.AutoPause,
                Reason = $"Duplicate content detected ({duplicateCheck.SimilarityScore:P0} similarity)",
                RiskLevel = AutomationRiskLevel.Medium,
                AffectedResource = "Content generation"
            });
        }

        // Classify overall risk
        result.OverallRisk = ClassifyRisk(result.Actions);
        result.CheckedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Safety check for tenant {TenantId}: {RiskLevel}, {ActionCount} actions recommended",
            tenantId, result.OverallRisk, result.Actions.Count);

        return result;
    }

    /// <summary>
    /// Auto-pauses campaigns that have triggered safety rules.
    /// </summary>
    public async Task<int> EnforceSafetyActionsAsync(Guid tenantId, SafetyCheckResult checkResult)
    {
        int actionsApplied = 0;

        foreach (var action in checkResult.Actions)
        {
            switch (action.ActionType)
            {
                case SafetyActionType.AutoPause:
                    var pausedCount = await PauseCampaignsAsync(tenantId, action.Reason);
                    actionsApplied += pausedCount;
                    _logger.LogWarning("Auto-paused {Count} campaigns for tenant {TenantId}: {Reason}",
                        pausedCount, tenantId, action.Reason);
                    break;

                case SafetyActionType.Rollback:
                    var rolledBack = await RollbackRecentChangesAsync(tenantId, action.Reason);
                    actionsApplied += rolledBack;
                    _logger.LogWarning("Rolled back {Count} recent changes for tenant {TenantId}: {Reason}",
                        rolledBack, tenantId, action.Reason);
                    break;

                case SafetyActionType.QueueForReview:
                    await CreateEscalationAsync(tenantId, action);
                    actionsApplied++;
                    break;
            }
        }

        return actionsApplied;
    }

    private async Task<TrafficCheckResult> CheckTrafficDropAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;

        // Compare last 24h vs previous 24h of workflow executions as a proxy for "traffic"
        var recentCount = await _context.WorkflowExecutions
            .CountAsync(e => e.TenantId == tenantId && e.StartedAt >= now.AddHours(-24));

        var previousCount = await _context.WorkflowExecutions
            .CountAsync(e => e.TenantId == tenantId
                          && e.StartedAt >= now.AddHours(-48)
                          && e.StartedAt < now.AddHours(-24));

        if (previousCount == 0)
            return new TrafficCheckResult { ShouldPause = false, DropPercentage = 0 };

        var dropPercentage = 1.0 - ((double)recentCount / previousCount);

        return new TrafficCheckResult
        {
            ShouldPause = dropPercentage >= TrafficDropPauseThreshold,
            DropPercentage = dropPercentage,
            RecentCount = recentCount,
            PreviousCount = previousCount
        };
    }

    private async Task<ErrorCheckResult> CheckErrorRateAsync(Guid tenantId, Guid? campaignId)
    {
        var since = DateTime.UtcNow.AddHours(-6);

        var query = _context.WorkflowExecutions
            .Where(e => e.TenantId == tenantId && e.StartedAt >= since);

        var totalExecutions = await query.CountAsync();
        if (totalExecutions < 10) // Don't evaluate with too few samples
            return new ErrorCheckResult { ShouldRollback = false, ErrorRate = 0 };

        var failedExecutions = await query.CountAsync(e => e.Status == "Failed" || e.Status == "CompensationFailed");
        var errorRate = (double)failedExecutions / totalExecutions;

        return new ErrorCheckResult
        {
            ShouldRollback = errorRate >= ErrorRateRollbackThreshold,
            ErrorRate = errorRate,
            TotalExecutions = totalExecutions,
            FailedExecutions = failedExecutions
        };
    }

    private async Task<DuplicateCheckResult> CheckDuplicateContentAsync(Guid tenantId)
    {
        // Simple check: compare recent workflow execution trigger data for duplicates
        var recentData = await _context.WorkflowExecutions
            .Where(e => e.TenantId == tenantId && e.StartedAt >= DateTime.UtcNow.AddHours(-24))
            .Select(e => e.TriggerEventData)
            .Take(100)
            .ToListAsync();

        if (recentData.Count < 5)
            return new DuplicateCheckResult { HasDuplicates = false, SimilarityScore = 0 };

        var uniqueCount = recentData.Distinct().Count();
        var similarityScore = 1.0 - ((double)uniqueCount / recentData.Count);

        return new DuplicateCheckResult
        {
            HasDuplicates = similarityScore >= DuplicateContentThreshold,
            SimilarityScore = similarityScore
        };
    }

    private async Task<int> PauseCampaignsAsync(Guid tenantId, string reason)
    {
        var activeWorkflows = await _context.Workflows
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .ToListAsync();

        foreach (var workflow in activeWorkflows)
        {
            workflow.IsActive = false;
        }

        if (activeWorkflows.Count > 0)
            await _context.SaveChangesAsync();

        return activeWorkflows.Count;
    }

    private async Task<int> RollbackRecentChangesAsync(Guid tenantId, string reason)
    {
        // Mark recent failed executions for compensation
        var recentFailed = await _context.WorkflowExecutions
            .Where(e => e.TenantId == tenantId
                     && e.Status == "Failed"
                     && !e.IsCompensated
                     && e.StartedAt >= DateTime.UtcNow.AddHours(-6))
            .ToListAsync();

        foreach (var execution in recentFailed)
        {
            execution.Status = "PendingCompensation";
        }

        if (recentFailed.Count > 0)
            await _context.SaveChangesAsync();

        return recentFailed.Count;
    }

    private async Task CreateEscalationAsync(Guid tenantId, SafetyAction action)
    {
        var severity = action.RiskLevel switch
        {
            AutomationRiskLevel.Critical => "Critical",
            AutomationRiskLevel.High => "High",
            AutomationRiskLevel.Medium => "Medium",
            _ => "Low"
        };

        _context.AIEscalations.Add(new AIEscalation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Module = "Automation",
            Reason = action.Reason,
            Severity = severity,
            RequiresApproval = true,
            IsResolved = false,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = $"{{\"affectedResource\":\"{action.AffectedResource}\"}}"
        });

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "Safety QueueForReview escalation created for tenant {TenantId}: [{Severity}] {Reason}",
            tenantId, severity, action.Reason);
    }

    private static AutomationRiskLevel ClassifyRisk(List<SafetyAction> actions)
    {
        if (actions.Count == 0) return AutomationRiskLevel.Low;
        if (actions.Any(a => a.RiskLevel == AutomationRiskLevel.Critical)) return AutomationRiskLevel.Critical;
        if (actions.Any(a => a.RiskLevel == AutomationRiskLevel.High)) return AutomationRiskLevel.High;
        if (actions.Any(a => a.RiskLevel == AutomationRiskLevel.Medium)) return AutomationRiskLevel.Medium;
        return AutomationRiskLevel.Low;
    }
}

internal class TrafficCheckResult
{
    public bool ShouldPause { get; set; }
    public double DropPercentage { get; set; }
    public int RecentCount { get; set; }
    public int PreviousCount { get; set; }
}

internal class ErrorCheckResult
{
    public bool ShouldRollback { get; set; }
    public double ErrorRate { get; set; }
    public int TotalExecutions { get; set; }
    public int FailedExecutions { get; set; }
}

internal class DuplicateCheckResult
{
    public bool HasDuplicates { get; set; }
    public double SimilarityScore { get; set; }
}
