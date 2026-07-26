using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class AdCampaignService : IAdCampaignService
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public AdCampaignService(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    private IAdPlatformService GetPlatformService(string platform)
    {
        return _serviceProvider.GetKeyedService<IAdPlatformService>(platform) 
            ?? throw new NotSupportedException($"Platform {platform} is not supported.");
    }

    public async Task<IEnumerable<AdCampaign>> GetActiveCampaignsAsync(Guid tenantId)
    {
        return await _context.AdCampaigns
            .Where(c => c.TenantId == tenantId && c.Status == "Active")
            .ToListAsync();
    }

    public async Task<bool> SyncPlatformCampaignsAsync(Guid tenantId, string platform)
    {
        var platformService = GetPlatformService(platform);
        var platformCampaigns = await platformService.GetCampaignsAsync(tenantId);
        
        var adAccount = await _context.AdAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Platform == platform && a.IsConnected);

        if (adAccount == null) return false;

        foreach (var pc in platformCampaigns)
        {
            var existing = await _context.AdCampaigns
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ExternalCampaignId == pc.ExternalId);

            if (existing == null)
            {
                _context.AdCampaigns.Add(new AdCampaign
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AdAccountId = adAccount.Id,
                    ExternalCampaignId = pc.ExternalId,
                    Name = pc.Name,
                    Platform = platform,
                    Status = pc.Status,
                    DailyBudget = pc.Budget,
                    StartDate = DateTime.UtcNow
                });
            }
            else
            {
                existing.Name = pc.Name;
                existing.Status = pc.Status;
                existing.DailyBudget = pc.Budget;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCampaignStatusAsync(Guid tenantId, Guid campaignId, string status)
    {
        var campaign = await _context.AdCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.TenantId == tenantId);

        if (campaign == null) return false;

        var platformService = GetPlatformService(campaign.Platform);
        var success = await platformService.UpdateCampaignStatusAsync(tenantId, campaign.ExternalCampaignId, status);

        if (success)
        {
            campaign.Status = status;
            await _context.SaveChangesAsync();
        }

        return success;
    }

    public async Task<AdMetricsDto> GetCampaignPerformanceAsync(Guid tenantId, Guid campaignId, DateTime from, DateTime to)
    {
        var campaign = await _context.AdCampaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.TenantId == tenantId);

        if (campaign == null) return new AdMetricsDto();

        var platformService = GetPlatformService(campaign.Platform);
        return await platformService.GetMetricsAsync(tenantId, campaign.ExternalCampaignId, from, to);
    }

    public async Task<decimal> GetTotalAdSpendAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var accounts = await _context.AdAccounts
            .Where(a => a.TenantId == tenantId && a.IsConnected)
            .ToListAsync();

        decimal totalSpend = 0;

        foreach (var account in accounts)
        {
            var campaigns = await _context.AdCampaigns
                .Where(c => c.AdAccountId == account.Id)
                .ToListAsync();

            var platformService = GetPlatformService(account.Platform);
            
            foreach (var campaign in campaigns)
            {
                var metrics = await platformService.GetMetricsAsync(tenantId, campaign.ExternalCampaignId, from, to);
                totalSpend += metrics.Spend;
            }
        }

        return totalSpend;
    }
}
