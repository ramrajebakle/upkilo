namespace Upkilo.Core.Entities;

/// <summary>
/// Feature privileges and limits for a subscription plan
/// </summary>
public class PlanFeatures
{
    // Core limits
    public int MaxLocations { get; set; } = 1;
    public int MaxStaff { get; set; } = 1;
    public int MaxBookingsPerMonth { get; set; } = 50;
    public int MaxClients { get; set; } = -1; // -1 = unlimited
    public int MaxServices { get; set; } = 10;
    public int MaxConcurrentBookings { get; set; } = 10; // New: Tier-based concurrency
    public string SchedulingPriority { get; set; } = "Normal"; // Low, Normal, High

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
    // When true, "Powered by Upkilo" branding is shown on public booking pages (viral loop)
    public bool ShowPoweredByBranding { get; set; } = true;

    // Governance
    public int AuditLogRetentionDays { get; set; } = 90;
    public List<string> AllowedAiModels { get; set; } = new() { "claude-haiku-4-5-20251001", "claude-sonnet-4-6" };

    // Usage credits
    public int MonthlySmsTier { get; set; } = 0; // 0, 100, 500, unlimited (-1)
    public int MonthlyAiCredits { get; set; } = 0;
    public decimal AiMonthlyBudget { get; set; } = 0m;
    public int StorageGb { get; set; } = 1;

    // Per-seat pricing
    public decimal ExtraStaffPrice { get; set; } = 5m;
    public decimal ExtraLocationPrice { get; set; } = 19m;
}

/// <summary>
/// Tenant subscription history and usage tracking
/// </summary>
/// <summary>
/// Tenant subscription history and usage tracking
/// </summary>
public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? PricingPlanId { get; set; } // Dynamic PricingPlan
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;
    public BillingInterval BillingInterval { get; set; } = BillingInterval.Monthly;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? ResumeAt { get; set; }

    // Stripe references
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripePaymentMethodId { get; set; }

    // Usage this period
    public int BookingsUsed { get; set; }
    public int SmsUsed { get; set; }
    public int AiCreditsUsed { get; set; }
    public long StorageUsedBytes { get; set; }

    // Expansion billing
    public int ExtraStaffCount { get; set; }
    public int ExtraLocationCount { get; set; }

    // Convenience aliases
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Alert tracking
    public int LastAlertThreshold { get; set; }

    // AI Governance
    public decimal AiMonthlyBudget { get; set; } = 0; // <=0 means no access, must be strictly positive for access
    public int AiLastAlertThreshold { get; set; }
    public int AuditLogRetentionDays { get; set; } = 90;
    public List<string> AllowedAiModels { get; set; } = new() { "claude-haiku-4-5-20251001", "claude-sonnet-4-6" };

    // Navigation
    public Tenant? Tenant { get; set; }
    public Upkilo.Core.Entities.PricingPlan? PricingPlan { get; set; }
}

public enum SubscriptionStatus
{
    Trialing,
    Trial,       // Alias for Trialing (used by Stripe mapping)
    Active,
    PastDue,
    Paused,
    Suspended,   // Auto-suspended due to non-payment
    Cancelled,
    Expired
}

public enum BillingInterval
{
    Monthly,
    Annual
}

public enum DiscountType
{
    Percentage,
    FixedAmount,
    FreeTrial
}
