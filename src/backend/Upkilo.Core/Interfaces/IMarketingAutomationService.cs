using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IMarketingAutomationService
{
    // Onboarding
    Task<MarketingConfig> OnboardAsync(Guid tenantId, string businessUrl, string primaryGoal, string? targetRegions);

    // SEO Agent
    Task<SeoAnalysis> AnalyzePageAsync(Guid tenantId, string pageUrl);

    // Content Agent
    Task<GeneratedContent> GenerateBlogPostAsync(Guid tenantId, string topic, string[]? keywords);
    Task<GeneratedContent> GenerateFaqAsync(Guid tenantId, string topic);

    // Distribution Agent
    Task<SocialPost> GenerateSocialPostAsync(Guid tenantId, string platform, string topic);

    // Analytics Agent
    Task<MarketingDashboardDto> GetDashboardAsync(Guid tenantId);
    Task<IEnumerable<MarketingForecast>> GetForecastsAsync(Guid tenantId, int horizonDays);
    Task<bool> SyncAnalyticsFromExternalAsync(Guid tenantId);

    // Lead Optimizer
    Task<ConversionAnalysis> OptimizeConversionsAsync(Guid tenantId);

    // Discovery Agent
    Task<AIDiscoveryReport> PerformDiscoveryScanAsync(Guid tenantId);

    // Safety
    Task<IEnumerable<AgentAction>> GetRecentActionsAsync(Guid tenantId, int count = 20);
}

public class MarketingDashboardDto
{
    public decimal TrafficGrowthPercent { get; set; }
    public int LeadsCapturedThisMonth { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal RevenueFromMarketing { get; set; }
    public int ContentPublished { get; set; }
    public int SocialPostsPublished { get; set; }
    public List<AgentStatusDto> AgentStatuses { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<AttributionDto> Attribution { get; set; } = new();
}

public class AttributionDto
{
    public string Channel { get; set; } = string.Empty;
    public int Value { get; set; }

    public AttributionDto() { }

    public AttributionDto(string channel, int value)
    {
        Channel = channel;
        Value = value;
    }
}

public class AgentStatusDto
{
    public string AgentName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ActionsToday { get; set; }
    public DateTime? LastRunAt { get; set; }
}
