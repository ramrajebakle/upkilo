using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IAdPlatformService
{
    string PlatformName { get; }
    Task<bool> ConnectAccountAsync(Guid tenantId, string authCode);
    Task<IEnumerable<AdCampaignDto>> GetCampaignsAsync(Guid tenantId);
    Task<bool> UpdateCampaignStatusAsync(Guid tenantId, string externalId, string status);
    Task<AdMetricsDto> GetMetricsAsync(Guid tenantId, string externalId, DateTime from, DateTime to);
}

public class AdCampaignDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Budget { get; set; }
}

public class AdMetricsDto
{
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public decimal Spend { get; set; }
    public decimal Conversions { get; set; }
    public decimal Roas => Spend > 0 ? (Conversions * 100) / Spend : 0; // Simplified ROAS
}
