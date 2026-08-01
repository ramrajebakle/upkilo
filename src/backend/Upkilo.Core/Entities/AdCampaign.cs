using System;

namespace Upkilo.Core.Entities;

public class AdCampaign : TenantEntity
{
    public Guid AdAccountId { get; set; }
    public string ExternalCampaignId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // Meta, Google, LinkedIn
    public string Status { get; set; } = "Paused"; // Active, Paused, Archived
    public decimal DailyBudget { get; set; }
    public decimal TotalBudget { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Objective { get; set; } // Leads, Conversions, BrandAwareness
    public string? OptimizationGoal { get; set; }

    // Navigation
    public virtual AdAccount? AdAccount { get; set; }
}
