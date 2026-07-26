using System;

namespace Upkilo.Core.Entities;

public class MarketingConfig : TenantEntity
{
    public string BusinessUrl { get; set; } = string.Empty;
    public string? IndustryNiche { get; set; }
    public string? TargetAudience { get; set; } // JSON
    public string PrimaryGoal { get; set; } = "leads"; // leads, sales, bookings
    public string? TargetRegions { get; set; } // JSON array
    public bool IsAutonomousMode { get; set; }
    public bool IsOnboarded { get; set; }
    public DateTime? LastCrawlAt { get; set; }
}
