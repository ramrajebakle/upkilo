using System;

namespace Upkilo.Core.Entities;

public enum BillingCycle
{
    Monthly,
    Annual,
    OneTime
}

public class PlanPrice : BaseEntity
{
    public Guid PricingPlanId { get; set; }
    public string? StripePriceId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public BillingCycle Cycle { get; set; }

    // Navigation
    public PricingPlan? PricingPlan { get; set; }
}
