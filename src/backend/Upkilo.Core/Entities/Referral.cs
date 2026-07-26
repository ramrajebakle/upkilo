namespace Upkilo.Core.Entities;

/// <summary>
/// Referral program: tracks referrals between tenants.
/// When a tenant refers another business, both get $50 credit
/// after the referred tenant activates a paid plan.
/// </summary>
public class Referral : BaseEntity
{
    public Guid ReferrerTenantId { get; set; }    // Tenant who referred
    public Guid? ReferredTenantId { get; set; }   // Tenant who signed up (set after activation)
    public string ReferralCode { get; set; } = string.Empty;  // Unique shareable code
    public string? ReferredEmail { get; set; }    // Email of referred person
    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;
    public decimal ReferrerCreditAmount { get; set; } = 50.00m;
    public decimal ReferredCreditAmount { get; set; } = 50.00m;
    public bool ReferrerCredited { get; set; }
    public bool ReferredCredited { get; set; }
    public DateTime? ActivatedAt { get; set; }    // When referred tenant activated paid plan
    public DateTime? CreditedAt { get; set; }     // When credits were applied
    public DateTime ExpiresAt { get; set; }       // Referral link expiry (90 days)

    // Navigation
    public Tenant? ReferrerTenant { get; set; }
    public Tenant? ReferredTenant { get; set; }
}

public enum ReferralStatus
{
    Pending,        // Link shared but not used
    SignedUp,       // Referred user registered
    Activated,      // Referred user on paid plan
    Credited,       // Both parties received credit
    Expired         // Referral link expired
}
