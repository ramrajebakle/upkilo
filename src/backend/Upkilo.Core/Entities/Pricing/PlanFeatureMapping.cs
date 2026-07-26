using System;

namespace Upkilo.Core.Entities;

public class PlanFeatureMapping : BaseEntity
{
    public Guid PricingPlanId { get; set; }
    public Guid PricingFeatureId { get; set; }
    
    public bool IsEnabled { get; set; }
    public int? NumericLimit { get; set; }
    public string? TextValue { get; set; }

    // Navigation
    public PricingPlan? PricingPlan { get; set; }
    public PricingFeature? PricingFeature { get; set; }
}
