namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks individual commissions earned by affiliate/agency partners.
/// Distinct from StaffCommission which tracks per-staff earnings.
/// </summary>
public class AffiliateCommission : BaseEntity
{
    public Guid PartnerAccountId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? PaymentId { get; set; }
    public string Source { get; set; } = "Subscription"; // Subscription, Booking, Referral
    public decimal GrossAmount { get; set; }
    public decimal CommissionRate { get; set; } // e.g. 0.20 for 20%
    public decimal CommissionAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public AffiliateCommissionStatus Status { get; set; } = AffiliateCommissionStatus.Pending;
    public Guid? PayoutId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual PartnerAccount? PartnerAccount { get; set; }
    public virtual AffiliatePayout? Payout { get; set; }
}

/// <summary>
/// Manages payout batches to affiliate/agency partners.
/// </summary>
public class AffiliatePayout : BaseEntity
{
    public Guid PartnerAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PayoutMethod { get; set; } = "Stripe"; // Stripe, PayPal, Wire
    public string? TransactionReference { get; set; }
    public AffiliatePayoutStatus Status { get; set; } = AffiliatePayoutStatus.Scheduled;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual PartnerAccount? PartnerAccount { get; set; }
    public virtual ICollection<AffiliateCommission> Commissions { get; set; } = new List<AffiliateCommission>();
}

public enum AffiliateCommissionStatus
{
    Pending,
    Approved,
    PaidOut,
    Voided
}

public enum AffiliatePayoutStatus
{
    Scheduled,
    Processing,
    Completed,
    Failed,
    Cancelled
}
