using System;
using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Service for interacting with external Marketing APIs (Google Search Console, Bing, LinkedIn, Twitter).
/// </summary>
public interface IMarketingIntegrationService
{
    // Search Content Submission
    Task<bool> SubmitToIndexAsync(Guid tenantId, string pageUrl, string platform); // Google, Bing
    
    // Search Performance Data
    Task<SearchAnalyticsResult> GetSearchPerformanceAsync(Guid tenantId, DateTime startDate, DateTime endDate);

    // Social Media Posting
    Task<string> PostSocialContentAsync(Guid tenantId, string platform, string content, string? mediaUrl = null);

    // Connection Management
    Task<bool> IsAppConnectedAsync(Guid tenantId, string platform);

    // Analytics Synchronization (GA4)
    Task<bool> SyncAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
}

public class SearchAnalyticsResult
{
    public int TotalImpressions { get; set; }
    public int TotalClicks { get; set; }
    public double AveragePosition { get; set; }
    public double AverageCtr { get; set; }
}
