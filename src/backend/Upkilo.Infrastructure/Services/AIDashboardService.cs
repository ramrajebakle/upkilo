using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class AIDashboardService : IAIDashboardService
{
    private readonly AppDbContext _context;

    public AIDashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AIDecisionLogDto>> GetDecisionLogsAsync(Guid tenantId, int count = 20)
    {
        return await _context.AIDecisionLogs
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .Select(l => new AIDecisionLogDto
            {
                Id = l.Id,
                AgentName = l.AgentName,
                DecisionType = l.DecisionType,
                OutputDecision = l.OutputDecision,
                ConfidenceScore = l.ConfidenceScore,
                CreatedAt = l.CreatedAt,
                RequiresHumanReview = l.RequiresHumanReview,
                IsApproved = l.IsApproved
            })
            .ToListAsync();
    }

    public async Task<AIDashboardMetricsDto> GetDashboardMetricsAsync(Guid tenantId)
    {
        var logs = await _context.AIDecisionLogs
            .Where(l => l.TenantId == tenantId)
            .ToListAsync();

        var usageLogs = await _context.AIUsageLogs
            .Where(l => l.TenantId == tenantId)
            .ToListAsync();

        return new AIDashboardMetricsDto
        {
            TotalDecisions = logs.Count,
            PendingReviews = logs.Count(l => l.RequiresHumanReview && !l.IsApproved),
            AvgConfidence = logs.Any() ? logs.Average(l => l.ConfidenceScore) : 0,
            TotalCost = usageLogs.Sum(l => l.Cost),
            TotalTokens = usageLogs.Sum(l => l.InputTokens + l.OutputTokens),
            DecisionsByAgent = logs.GroupBy(l => l.AgentName)
                                   .ToDictionary(g => g.Key, g => g.Count()),
            UsageByModel = usageLogs.GroupBy(l => l.Model)
                                    .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<IEnumerable<AITokenUsageDto>> GetTokenUsageTrendsAsync(Guid tenantId, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        return await _context.AIUsageLogs
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= cutoff)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new AITokenUsageDto
            {
                Date = g.Key,
                InputTokens = g.Sum(l => l.InputTokens),
                OutputTokens = g.Sum(l => l.OutputTokens),
                Cost = g.Sum(l => l.Cost)
            })
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    public async Task<bool> ApproveDecisionAsync(Guid tenantId, Guid decisionId, Guid userId)
    {
        var log = await _context.AIDecisionLogs
            .FirstOrDefaultAsync(l => l.Id == decisionId && l.TenantId == tenantId);

        if (log == null) return false;

        log.IsApproved = true;
        log.ReviewedBy = userId;
        log.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task LogDecisionAsync(Guid tenantId, string agentName, string decisionType, string input, string output, decimal confidence, string model = "gpt-4", int inputTokens = 0, int outputTokens = 0, Guid? entityId = null, string? entityType = null)
    {
        var log = new AIDecisionLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AgentName = agentName,
            DecisionType = decisionType,
            InputData = input,
            OutputDecision = output,
            ConfidenceScore = confidence,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            RelatedEntityId = entityId,
            RelatedEntityType = entityType,
            RequiresHumanReview = confidence < 0.8m,
            CreatedAt = DateTime.UtcNow
        };

        _context.AIDecisionLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
