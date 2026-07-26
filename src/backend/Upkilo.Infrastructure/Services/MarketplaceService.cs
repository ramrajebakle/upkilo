using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class MarketplaceService : IMarketplaceService
{
    private readonly AppDbContext _context;

    public MarketplaceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BusinessListing>> GetFeaturedListingsAsync(string? city, string? category, string? search)
    {
        var query = _context.BusinessListings.IgnoreQueryFilters()
            .Where(b => b.IsFeatured && b.IsActive && !b.IsDeleted);

        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(b => b.City == city);
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(b => b.Category == category);
        }
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => b.BusinessName.Contains(search) || b.Description.Contains(search));
        }

        return await query.OrderByDescending(b => b.PremiumScore).Take(20).ToListAsync();
    }

    public async Task<decimal> CalculateLeadFeesAsync(Guid tenantId)
    {
        // Real logic: sum ConversionEvents of type 'Lead' tracking source 'Marketplace'
        // where the lead was generated but not yet billed.
        var leadCount = await _context.ConversionEvents
            .Where(c => c.TenantId == tenantId && c.Source == "Marketplace" && c.EventCategory == "lead" && !c.IsBilled)
            .CountAsync();
            
        return leadCount * 2.50m; // $2.50 per marketplace lead
    }

    public async Task MarkLeadsAsBilledAsync(Guid tenantId)
    {
        var unbilledLeads = await _context.ConversionEvents
            .Where(c => c.TenantId == tenantId && c.Source == "Marketplace" && c.EventCategory == "lead" && !c.IsBilled)
            .ToListAsync();

        foreach (var lead in unbilledLeads)
        {
            lead.IsBilled = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> PurchasePremiumBadgeAsync(Guid tenantId)
    {
        var listing = await _context.BusinessListings
            .FirstOrDefaultAsync(b => b.TenantId == tenantId);
        
        if (listing == null) return false;

        listing.IsFeatured = true;
        listing.PremiumScore += 50; // Boost visibility
        listing.IsVerified = true; // Premium badge implies verification

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetAdRevenueShareMetricsAsync()
    {
        var activeAds = await _context.AdCampaigns.IgnoreQueryFilters()
            .Where(a => a.Status == "Active" && !a.IsDeleted)
            .ToListAsync();

        var totalAdSpend = activeAds.Sum(a => a.DailyBudget * 30); // simplistic monthly projection
        var platformShare = 0.15m; // 15% revenue share as per Task 381
        
        var platformRevenue = totalAdSpend * platformShare;
        var partnerPayouts = totalAdSpend * (1 - platformShare);

        return new
        {
            ActiveCampaignsCount = activeAds.Count,
            MonthlyProjectedSpend = totalAdSpend,
            PlatformRevenueShare = platformRevenue,
            PartnerPayouts = partnerPayouts,
            RevenueSharePercentage = 15
        };
    }
}
