using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class GrowthService : IGrowthService
{
    private readonly AppDbContext _context;

    public GrowthService(AppDbContext context)
    {
        _context = context;
    }

    // === DIRECTORY ===

    public async Task<IEnumerable<BusinessListing>> SearchDirectoryAsync(string? city, string? category, int page = 1, int pageSize = 20)
    {
        var query = _context.BusinessListings.Where(b => b.IsActive);

        if (!string.IsNullOrEmpty(city))
            query = query.Where(b => b.City != null && b.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(b => b.Category.ToLower().Contains(category.ToLower()));

        return await query
            .OrderByDescending(b => b.IsFeatured)
            .ThenByDescending(b => b.AverageRating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<BusinessListing?> GetListingBySlugAsync(string slug)
    {
        return await _context.BusinessListings.FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
    }

    public async Task<bool> ToggleFeaturedAsync(Guid tenantId, Guid listingId, bool featured)
    {
        var listing = await _context.BusinessListings.FirstOrDefaultAsync(b => b.Id == listingId && b.TenantId == tenantId);
        if (listing == null) return false;
        listing.IsFeatured = featured;
        await _context.SaveChangesAsync();
        return true;
    }

    // === REFERRALS ===

    public async Task<string> GenerateReferralCodeAsync(Guid tenantId)
    {
        var existing = await _context.ReferralRecords.FirstOrDefaultAsync(r => r.ReferrerId == tenantId);
        if (existing != null) return existing.ReferralCode;

        var code = $"UPKILO-{tenantId.ToString()[..8].ToUpper()}";
        var record = new ReferralRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReferrerId = tenantId,
            ReferralCode = code,
            ReferredEmail = string.Empty,
            Status = "Active"
        };
        _context.ReferralRecords.Add(record);
        await _context.SaveChangesAsync();
        return code;
    }

    public async Task<bool> RedeemReferralAsync(string code, string referredEmail)
    {
        var referral = await _context.ReferralRecords.FirstOrDefaultAsync(r => r.ReferralCode == code && r.Status == "Active");
        if (referral == null) return false;

        var newReferral = new ReferralRecord
        {
            Id = Guid.NewGuid(),
            TenantId = referral.TenantId,
            ReferrerId = referral.ReferrerId,
            ReferralCode = code,
            ReferredEmail = referredEmail,
            Status = "SignedUp",
            ReferrerCredit = 50.00m,
            ReferredCredit = 50.00m
        };
        _context.ReferralRecords.Add(newReferral);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ReferralSummaryDto> GetReferralSummaryAsync(Guid tenantId)
    {
        var referrals = await _context.ReferralRecords
            .Where(r => r.ReferrerId == tenantId)
            .ToListAsync();

        return new ReferralSummaryDto
        {
            ReferralCode = referrals.FirstOrDefault()?.ReferralCode ?? string.Empty,
            TotalReferrals = referrals.Count(r => r.Status != "Active"),
            QualifiedReferrals = referrals.Count(r => r.Status == "Qualified" || r.Status == "Rewarded"),
            TotalCreditsEarned = referrals.Where(r => r.Status == "Rewarded").Sum(r => r.ReferrerCredit)
        };
    }

    // === PARTNERS ===

    public async Task<PartnerAccount?> GetPartnerAccountAsync(Guid tenantId)
    {
        return await _context.PartnerAccounts.FirstOrDefaultAsync(p => p.TenantId == tenantId);
    }

    public async Task<bool> RegisterPartnerAsync(Guid tenantId, string name, string email, string type)
    {
        var existing = await _context.PartnerAccounts.AnyAsync(p => p.TenantId == tenantId);
        if (existing) return false;

        _context.PartnerAccounts.Add(new PartnerAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PartnerName = name,
            ContactEmail = email,
            PartnerType = type,
            RevenueSharePercent = 20.0m
        });
        await _context.SaveChangesAsync();
        return true;
    }
}
