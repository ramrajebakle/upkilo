using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public enum FeatureType
{
    Boolean,    // e.g., Has API Access
    Numeric,    // e.g., Max Users = 50
    Text        // e.g., Support Level = Priority
}

public class PricingFeature : BaseEntity
{
    public string Key { get; set; } = string.Empty; // e.g., "ai_workflows"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeatureType Type { get; set; }

    // Navigation
    public ICollection<PlanFeatureMapping> PlanMappings { get; set; } = new List<PlanFeatureMapping>();
}
