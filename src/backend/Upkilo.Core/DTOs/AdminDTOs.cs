namespace Upkilo.Core.DTOs;

public class UpdateTenantStatusRequest
{
    public string Status { get; set; } = string.Empty; // active, suspended, cancelled
    public string? Reason { get; set; }
}

public class ChangeTenantPlanRequest
{
    public string PlanId { get; set; } = string.Empty;
    public bool Immediately { get; set; }
}

public class CreatePlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tier { get; set; } = "Starter"; // Free, Starter, Professional, Business, Enterprise
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string StripePriceIdMonthly { get; set; } = string.Empty;
    public string StripePriceIdAnnual { get; set; } = string.Empty;
    public string? StripeExtraStaffPriceId { get; set; }
    public string? StripeExtraLocationPriceId { get; set; }
    public string? StripeAiUsagePriceId { get; set; }
    public int TrialDays { get; set; } = 14;
    public int SortOrder { get; set; }
    
    // Feature limits
    public int MaxLocations { get; set; } = 1;
    public int MaxStaff { get; set; } = 1;
    public int MaxBookingsPerMonth { get; set; } = 50;
    public int MaxClients { get; set; } = -1;
    public int MaxServices { get; set; } = 10;
    public int MaxConcurrentBookings { get; set; } = 10;
    public string SchedulingPriority { get; set; } = "Normal";
    
    // Feature flags
    public bool OnlineBooking { get; set; } = true;
    public bool EmailReminders { get; set; } = true;
    public bool SmsReminders { get; set; } = false;
    public bool CalendarSync { get; set; } = false;
    public bool CustomBranding { get; set; } = false;
    public bool WhiteLabelDomain { get; set; } = false;
    public bool AdvancedReporting { get; set; } = false;
    public bool ApiAccess { get; set; } = false;
    public bool Webhooks { get; set; } = false;
    public bool MultiLocation { get; set; } = false;
    public bool TeamManagement { get; set; } = false;
    public bool MarketplaceListing { get; set; } = false;
    public bool AiFeatures { get; set; } = false;
    public bool PrioritySupport { get; set; } = false;
    public bool SlaGuarantee { get; set; } = false;
    
    public int AuditLogRetentionDays { get; set; } = 90;
    public List<string> AllowedAiModels { get; set; } = new();
    
    public int MonthlySmsTier { get; set; } = 0;
    public int MonthlyAiCredits { get; set; } = 0;
    public decimal AiMonthlyBudget { get; set; } = 0m;
    public int StorageGb { get; set; } = 1;
    
    public decimal ExtraStaffPrice { get; set; } = 5m;
    public decimal ExtraLocationPrice { get; set; } = 19m;
}

public class UpdatePlanRequest : CreatePlanRequest
{
    public bool IsActive { get; set; } = true;
}


public class UpdateFeatureFlagRequest
{
    public bool? Enabled { get; set; }
    public int? RolloutPercent { get; set; }
    public List<Guid>? TenantIds { get; set; }
}

public class SendAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, critical
    public List<string>? TargetPlans { get; set; }
}
