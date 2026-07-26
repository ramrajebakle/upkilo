using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IGrowthService
{
    // Directory
    Task<IEnumerable<BusinessListing>> SearchDirectoryAsync(string? city, string? category, int page = 1, int pageSize = 20);
    Task<BusinessListing?> GetListingBySlugAsync(string slug);
    Task<bool> ToggleFeaturedAsync(Guid tenantId, Guid listingId, bool featured);

    // Referrals
    Task<string> GenerateReferralCodeAsync(Guid tenantId);
    Task<bool> RedeemReferralAsync(string code, string referredEmail);
    Task<ReferralSummaryDto> GetReferralSummaryAsync(Guid tenantId);

    // Partners
    Task<PartnerAccount?> GetPartnerAccountAsync(Guid tenantId);
    Task<bool> RegisterPartnerAsync(Guid tenantId, string name, string email, string type);
}

public class ReferralSummaryDto
{
    public string ReferralCode { get; set; } = string.Empty;
    public int TotalReferrals { get; set; }
    public int QualifiedReferrals { get; set; }
    public decimal TotalCreditsEarned { get; set; }
}
