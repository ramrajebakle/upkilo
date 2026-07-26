using System;

namespace Upkilo.Core.Entities
{
    /// <summary>
    /// Tracks Stripe Connect payouts to staff/contractors
    /// </summary>
    public class StripePayout : TenantEntity
    {
        public Guid StaffId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string StripeTransferId { get; set; } = string.Empty;
        public string StripePayoutId { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, paid, failed, cancelled
        public DateTime? ArrivalDate { get; set; }
        public string? FailureCode { get; set; }
        public string? FailureMessage { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual StaffMember? Staff { get; set; }
    }
}
