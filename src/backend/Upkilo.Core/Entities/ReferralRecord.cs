using System;

namespace Upkilo.Core.Entities;

public class ReferralRecord : TenantEntity
{
    public Guid ReferrerId { get; set; } // Tenant that referred
    public Guid? ReferredTenantId { get; set; } // Tenant that was referred
    public string ReferralCode { get; set; } = string.Empty;
    public string ReferredEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, SignedUp, Qualified, Rewarded
    public decimal ReferrerCredit { get; set; } = 50.00m;
    public decimal ReferredCredit { get; set; } = 50.00m;
    public DateTime? QualifiedAt { get; set; }
    public DateTime? RewardedAt { get; set; }
}
