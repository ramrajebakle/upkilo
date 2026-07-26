using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public class PricingPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StripeProductId { get; set; }
    public bool IsActive { get; set; } = true;
    public int TrialDays { get; set; } = 14;
    public bool IsCustom { get; set; } = false; // Enterprise custom-quote plans

    // Metered overage Stripe price IDs (null = feature not available on this plan)
    public string? StripeAiUsagePriceId { get; set; }       // per AI action overage
    public string? StripeSmsOveragePriceId { get; set; }    // per SMS beyond tier
    public string? StripeExtraStaffPriceId { get; set; }    // per extra staff seat/mo
    public string? StripeExtraLocationPriceId { get; set; } // per extra location/mo

    // Navigation
    public ICollection<PlanPrice> Prices { get; set; } = new List<PlanPrice>();
    public ICollection<PlanFeatureMapping> FeatureMappings { get; set; } = new List<PlanFeatureMapping>();
}
