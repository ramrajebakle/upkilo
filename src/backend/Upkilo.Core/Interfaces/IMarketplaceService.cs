using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IMarketplaceService
{
    Task<IEnumerable<BusinessListing>> GetFeaturedListingsAsync(string? city, string? category, string? search);
    Task<decimal> CalculateLeadFeesAsync(Guid tenantId);
    Task MarkLeadsAsBilledAsync(Guid tenantId);
    Task<bool> PurchasePremiumBadgeAsync(Guid tenantId);
    Task<object> GetAdRevenueShareMetricsAsync();
}
