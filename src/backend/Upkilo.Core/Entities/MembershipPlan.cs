namespace Upkilo.Core.Entities;

public class MembershipPlan : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string BillingInterval { get; set; } = "monthly"; // monthly, yearly
    public int ServicesIncluded { get; set; } // -1 for unlimited
    public int DiscountPercent { get; set; }
    public string? FeaturesJson { get; set; } // JSON list of features
    public bool IsActive { get; set; } = true;
    
    // Stripe Integration
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }
    
    public ICollection<ClientMembership> Subscriptions { get; set; } = new List<ClientMembership>();
}
