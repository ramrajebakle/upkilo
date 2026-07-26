using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces
{
    public interface IPayoutService
    {
        Task<PayoutResult> CreateStaffPayoutAsync(Guid tenantId, Guid staffId, decimal amount, string currency = "USD");
        Task<IEnumerable<StripePayout>> GetStaffPayoutHistoryAsync(Guid staffId);
        Task<PayoutResult> ProcessCommissionPayoutsAsync(Guid tenantId);
        Task<string> GetStaffOnboardingUrlAsync(Guid tenantId, Guid staffId, string returnUrl);
        Task SyncPayoutStatusAsync(string stripePayoutId);
    }

    public class PayoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StripePayoutId { get; set; }
        public string? OnboardingUrl { get; set; }
    }
}
