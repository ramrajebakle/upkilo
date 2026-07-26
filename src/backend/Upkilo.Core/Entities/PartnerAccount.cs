using System;

namespace Upkilo.Core.Entities;

public class PartnerAccount : TenantEntity
{
    public string PartnerName { get; set; } = string.Empty;
    public string PartnerType { get; set; } = "Agency"; // Agency, Reseller, Affiliate
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public decimal RevenueSharePercent { get; set; } = 20.0m; // 20% default
    public decimal TotalEarnings { get; set; }
    public decimal PendingPayout { get; set; }
    public int ManagedAccounts { get; set; }
    public string Status { get; set; } = "Active"; // Active, Suspended, Pending
    public string? PayoutMethod { get; set; } // Stripe, PayPal, Wire
    public string? PayoutDetails { get; set; } // JSON
    public string? ReferralCode { get; set; } // Unique code for affiliate links e.g. upkilo.com/ref/[code]
    public string? StripeConnectAccountId { get; set; } // For Stripe Connect payouts
}
