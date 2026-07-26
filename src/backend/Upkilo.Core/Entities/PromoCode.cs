namespace Upkilo.Core.Entities;

/// <summary>
/// Promotional code / coupon for discounts on bookings or subscriptions.
/// Supports percentage and fixed amount discounts with usage limits.
/// </summary>
public class PromoCode : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromoType DiscountType { get; set; } = PromoType.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int? MaxUses { get => UsageLimit; set => UsageLimit = value; } // Alias
    public int? MaxUsagePerCustomer { get; set; } = 1;
    public int TimesUsed { get; set; }
    public int CurrentUses { get => TimesUsed; set => TimesUsed = value; } // Alias
    public bool IsActive { get; set; } = true;
    public bool FirstTimeOnly { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ValidFrom { get => StartsAt; set => StartsAt = value; } // Alias
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ValidUntil { get => ExpiresAt; set => ExpiresAt = value; } // Alias
    public string? ApplicableServices { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public enum PromoType
{
    Percentage,      // e.g., 20% off
    FixedAmount,     // e.g., $10 off
    FreeTrial        // 100% off for trial period
}

/// <summary>
/// Record of promo code redemptions
/// </summary>
public class PromoRedemption : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid PromoCodeId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
    public decimal DiscountApplied { get; set; }
    
    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual PromoCode? PromoCode { get; set; }
    public virtual Client? Client { get; set; }
}
