using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Upkilo.Infrastructure.Data;
namespace Upkilo.Infrastructure.Services;

public class MetaAdsService : IAdPlatformService
{
    private readonly AppDbContext _context;

    public MetaAdsService(AppDbContext context)
    {
        _context = context;
    }

    public string PlatformName => "Meta";

    public Task<bool> ConnectAccountAsync(Guid tenantId, string authCode)
    {
        return Task.FromResult(true);
    }

    public async Task<IEnumerable<AdCampaignDto>> GetCampaignsAsync(Guid tenantId)
    {
        var campaigns = await _context.AdCampaigns
            .Where(c => c.TenantId == tenantId && c.Platform == "Meta")
            .Select(c => new AdCampaignDto
            {
                ExternalId = c.ExternalCampaignId ?? Guid.NewGuid().ToString(),
                Name = c.Name,
                Status = c.Status,
                Budget = c.DailyBudget
            })
            .ToListAsync();

        return campaigns;
    }

    public Task<bool> UpdateCampaignStatusAsync(Guid tenantId, string externalId, string status)
    {
        return Task.FromResult(true);
    }

    public Task<AdMetricsDto> GetMetricsAsync(Guid tenantId, string externalId, DateTime from, DateTime to)
    {
        return Task.FromResult(new AdMetricsDto
        {
            Impressions = 15000,
            Clicks = 450,
            Spend = 125.50m,
            Conversions = 12.0m
        });
    }
}
