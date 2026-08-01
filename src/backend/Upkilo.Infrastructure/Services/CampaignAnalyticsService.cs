using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

public class CampaignAnalyticsService : ICampaignAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CampaignAnalyticsService> _logger;

    public CampaignAnalyticsService(AppDbContext context, ILogger<CampaignAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CampaignAnalytics?> GetAnalyticsAsync(Guid campaignId)
    {
        var analytics = await _context.CampaignAnalytics
            .FirstOrDefaultAsync(a => a.CampaignId == campaignId);

        if (analytics == null)
        {
            return new CampaignAnalytics { CampaignId = campaignId };
        }

        return analytics;
    }

    public async Task RecordEventAsync(Guid campaignId, string eventType, string? metadata = null)
    {
        var analytics = await _context.CampaignAnalytics
            .FirstOrDefaultAsync(a => a.CampaignId == campaignId);

        if (analytics == null)
        {
            analytics = new CampaignAnalytics
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                CreatedAt = DateTime.UtcNow
            };
            _context.CampaignAnalytics.Add(analytics);
        }

        var campaign = await _context.Campaigns.FindAsync(campaignId);
        var tenantId = campaign?.TenantId ?? Guid.Empty;

        switch (eventType.ToLower())
        {
            case "open":
                analytics.OpenedCount++;
                break;
            case "click":
                analytics.ClickedCount++;
                break;
            case "delivery":
                analytics.DeliveredCount++;
                break;
            case "bounce":
                analytics.BouncedCount++;
                break;
        }

        // Record granular metric for historical trending
        if (tenantId != Guid.Empty)
        {
            _context.Set<MarketingAnalytics>().Add(new MarketingAnalytics
            {
                TenantId = tenantId,
                MetricType = eventType,
                Source = "Campaign",
                Value = 1,
                RecordDate = DateTime.UtcNow,
                Insight = metadata
            });
        }

        // --- Real Timeline Aggregation ---
        var timeline = new List<TimelinePoint>();
        if (!string.IsNullOrEmpty(analytics.TimelineData))
        {
            try { timeline = JsonSerializer.Deserialize<List<TimelinePoint>>(analytics.TimelineData) ?? new List<TimelinePoint>(); }
            catch (JsonException ex) { _logger.LogWarning(ex, "Malformed TimelineData for campaign {CampaignId}; treating as empty", campaignId); }
        }
        var currentHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
        var point = timeline.FirstOrDefault(t => t.Timestamp == currentHour && t.Type == eventType);
        if (point == null)
            timeline.Add(new TimelinePoint { Timestamp = currentHour, Type = eventType, Count = 1 });
        else
            point.Count++;

        analytics.TimelineData = JsonSerializer.Serialize(timeline);

        // --- Real Device Data Aggregation ---
        if (!string.IsNullOrEmpty(metadata) && (metadata.Contains("Mobile") || metadata.Contains("Desktop") || metadata.Contains("Tablet")))
        {
            var devices = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(analytics.DeviceData))
            {
                try { devices = JsonSerializer.Deserialize<Dictionary<string, int>>(analytics.DeviceData) ?? new Dictionary<string, int>(); }
                catch (JsonException ex) { _logger.LogWarning(ex, "Malformed DeviceData for campaign {CampaignId}; treating as empty", campaignId); }
            }
            var deviceName = metadata;
            if (devices.ContainsKey(deviceName))
                devices[deviceName]++;
            else
                devices[deviceName] = 1;

            analytics.DeviceData = JsonSerializer.Serialize(devices);
        }

        analytics.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Recorded {EventType} for campaign {CampaignId}", eventType, campaignId);
    }

    public async Task<IEnumerable<TimelinePoint>> GetTimelineDataAsync(Guid campaignId, DateTime start, DateTime end)
    {
        var analytics = await _context.CampaignAnalytics
            .FirstOrDefaultAsync(a => a.CampaignId == campaignId);

        if (analytics != null && !string.IsNullOrEmpty(analytics.TimelineData))
        {
            try { return JsonSerializer.Deserialize<List<TimelinePoint>>(analytics.TimelineData) ?? new List<TimelinePoint>(); }
            catch (JsonException ex) { _logger.LogWarning(ex, "Malformed TimelineData for campaign {CampaignId}; returning empty", campaignId); }
        }
        return new List<TimelinePoint>();
    }

    public async Task<Dictionary<string, int>> GetDeviceStatsAsync(Guid campaignId)
    {
        var analytics = await _context.CampaignAnalytics
            .FirstOrDefaultAsync(a => a.CampaignId == campaignId);

        if (analytics != null && !string.IsNullOrEmpty(analytics.DeviceData))
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, int>>(analytics.DeviceData) ?? new Dictionary<string, int>(); }
            catch (JsonException ex) { _logger.LogWarning(ex, "Malformed DeviceData for campaign {CampaignId}; returning empty", campaignId); }
        }
        return new Dictionary<string, int>();
    }
}
