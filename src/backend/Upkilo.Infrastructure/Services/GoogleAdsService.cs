using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Upkilo.Infrastructure.Data;
namespace Upkilo.Infrastructure.Services;

public class GoogleAdsService : IAdPlatformService
{
    private readonly AppDbContext _context;

    public GoogleAdsService(AppDbContext context)
    {
        _context = context;
    }

    public string PlatformName => "Google";

    public Task<bool> ConnectAccountAsync(Guid tenantId, string authCode)
    {
        return Task.FromResult(true);
    }

    public async Task<IEnumerable<AdCampaignDto>> GetCampaignsAsync(Guid tenantId)
    {
        var campaigns = await _context.AdCampaigns
            .Where(c => c.TenantId == tenantId && c.Platform == "Google")
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
            Impressions = 5000,
            Clicks = 320,
            Spend = 85.00m,
            Conversions = 5.0m
        });
    }
}
