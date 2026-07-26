using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ILandingPageService
{
    Task<LandingPage> CreatePageAsync(Guid tenantId, string title, string slug, string htmlContent, Guid? campaignId);
    Task<LandingPage?> GetPageBySlugAsync(string slug);
    Task<IEnumerable<LandingPage>> GetPagesAsync(Guid tenantId);
    Task<bool> PublishPageAsync(Guid tenantId, Guid pageId);
    Task<bool> RecordViewAsync(Guid pageId);
    Task<bool> RecordConversionAsync(Guid pageId);
}

public interface IConversionTrackingService
{
    Task TrackEventAsync(Guid tenantId, ConversionEvent evt);
    Task<IEnumerable<ConversionEvent>> GetEventsAsync(Guid tenantId, DateTime from, DateTime to);
    Task<ConversionSummaryDto> GetSummaryAsync(Guid tenantId, DateTime from, DateTime to);
}

public class ConversionSummaryDto
{
    public int TotalEvents { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal BilledRevenue { get; set; }
    public decimal UnbilledRevenue { get; set; }
    public int LeadsCaptured { get; set; }
    public int UnbilledLeads { get; set; }
    public int BookingsFromAds { get; set; }
    public Dictionary<string, int> EventsByPlatform { get; set; } = new();
    public Dictionary<string, decimal> RevenueBySource { get; set; } = new();
}
