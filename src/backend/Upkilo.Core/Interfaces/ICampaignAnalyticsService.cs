using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ICampaignAnalyticsService
{
    /// <summary>
    /// Gets the current analytics for a specific campaign
    /// </summary>
    Task<CampaignAnalytics?> GetAnalyticsAsync(Guid campaignId);

    /// <summary>
    /// Records a campaign event (open, click, etc.)
    /// </summary>
    Task RecordEventAsync(Guid campaignId, string eventType, string? metadata = null);

    /// <summary>
    /// Generates hourly/daily timeline data for charts
    /// </summary>
    Task<IEnumerable<TimelinePoint>> GetTimelineDataAsync(Guid campaignId, DateTime start, DateTime end);

    /// <summary>
    /// Gets device/browser distribution for a campaign
    /// </summary>
    Task<Dictionary<string, int>> GetDeviceStatsAsync(Guid campaignId);
}

public class TimelinePoint
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
    public string Type { get; set; } = "open";
}
