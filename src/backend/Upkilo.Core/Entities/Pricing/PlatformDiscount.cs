using System;

namespace Upkilo.Core.Entities;

public class PlatformDiscount : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PlatformDiscountType Type { get; set; }
    public decimal Value { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int CurrentRedemptions { get; set; }
    
    // External ID (e.g., Stripe Coupon ID)
    public string? StripeCouponId { get; set; }
}

public enum PlatformDiscountType
{
    Percentage,
    FixedAmount
}
