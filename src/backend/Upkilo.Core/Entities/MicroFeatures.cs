using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Product catalog for e-commerce / shop module.
/// </summary>
public class Product : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public string? Barcode { get; set; }
    public bool RequiresShipping { get; set; } = true;
    public string ProductType { get; set; } = "Physical"; // Physical, Digital
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public bool TrackInventory { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Variants { get; set; } // JSON: size, color
    public string? DigitalFileUrl { get; set; }
    public decimal? Weight { get; set; }
}

/// <summary>
/// Shopping cart for e-commerce.
/// </summary>
public class CartItem : TenantEntity
{
    public Guid? ClientId { get; set; }
    public string? SessionId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? SelectedVariant { get; set; } // JSON
    public virtual Product? Product { get; set; }
}

/// <summary>
/// E-commerce order.
/// </summary>
public class Order : TenantEntity
{
    public Guid ClientId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled
    public string? ShippingAddress { get; set; } // JSON
    public string? TrackingNumber { get; set; }
    public string? PaymentIntentId { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

/// <summary>
/// Loyalty/rewards program for client retention.
/// </summary>
public class LoyaltyProgram : TenantEntity
{
    public string Name { get; set; } = "Rewards Program";
    public bool IsActive { get; set; } = true;
    public decimal PointsPerDollar { get; set; } = 1;
    public decimal PointsRedemptionRate { get; set; } = 100; // 100 points = $1
    public int ReferralBonusPoints { get; set; } = 500;
    public string Tiers { get; set; } = "[{\"name\":\"Bronze\",\"minPoints\":0},{\"name\":\"Silver\",\"minPoints\":500},{\"name\":\"Gold\",\"minPoints\":2000}]";
    public int? PointExpiryDays { get; set; }
}

/// <summary>
/// Client loyalty points balance and history.
/// </summary>
public class LoyaltyBalance : TenantEntity
{
    public Guid ClientId { get; set; }
    public int TotalPoints { get; set; }
    public int LifetimePoints { get; set; }
    public string CurrentTier { get; set; } = "Bronze";
    public int StampCount { get; set; } // For stamp card feature
    public virtual Client? Client { get; set; }
}

/// <summary>
/// A redeemable reward in the loyalty program.
/// </summary>
public class LoyaltyReward : TenantEntity
{
    public Guid LoyaltyProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PointsCost { get; set; }
    public string RewardType { get; set; } = "Discount"; // Discount, FreeService, Product, GiftCard
    public decimal? RewardValue { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaxRedemptions { get; set; }
    public int TimesRedeemed { get; set; }
}

// Note: PromoCode class is defined in PromoCode.cs (with PromoType enum and full features)


/// <summary>
/// Class/group booking definition.
/// </summary>
public class ClassDefinition : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxCapacity { get; set; } = 20;
    public int CurrentEnrolled { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public decimal Price { get; set; }
    public Guid? InstructorId { get; set; }
    public string? RecurrenceRule { get; set; } // iCal RRULE
    public string? Location { get; set; }
    public bool AllowWaitlist { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
