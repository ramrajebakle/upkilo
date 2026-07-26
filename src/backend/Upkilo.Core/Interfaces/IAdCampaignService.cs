using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IAdCampaignService
{
    Task<IEnumerable<AdCampaign>> GetActiveCampaignsAsync(Guid tenantId);
    Task<bool> SyncPlatformCampaignsAsync(Guid tenantId, string platform);
    Task<bool> UpdateCampaignStatusAsync(Guid tenantId, Guid campaignId, string status);
    Task<AdMetricsDto> GetCampaignPerformanceAsync(Guid tenantId, Guid campaignId, DateTime from, DateTime to);
    Task<decimal> GetTotalAdSpendAsync(Guid tenantId, DateTime from, DateTime to);
}
