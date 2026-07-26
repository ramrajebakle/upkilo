using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Subscription service interface for managing tenant subscriptions
/// </summary>
public interface ISubscriptionService
{
    // Plan management
    Task<IEnumerable<PricingPlan>> GetAllPricingPlansAsync();

    // Tenant subscription
    Task<Subscription?> GetSubscriptionAsync(Guid tenantId);
    Task<SubscriptionResult> CreateSubscriptionAsync(Guid tenantId, Guid planId, BillingInterval interval, string? promoCode = null);
    Task<SubscriptionResult> ChangeSubscriptionAsync(Guid tenantId, Guid newPlanId, BillingInterval? newInterval = null);
    Task<SubscriptionResult> CancelSubscriptionAsync(Guid tenantId, bool immediate = false);
    Task<SubscriptionResult> PauseSubscriptionAsync(Guid tenantId, DateTime? resumeAt = null);
    Task<SubscriptionResult> ResumeSubscriptionAsync(Guid tenantId);
    
    // Usage and limits
    Task<UsageSummary> GetUsageAsync(Guid tenantId);
    Task<bool> CheckFeatureAccessAsync(Guid tenantId, string featureName);
    Task<bool> CheckUsageLimitAsync(Guid tenantId, UsageType usageType, int amount = 1);
    Task IncrementUsageAsync(Guid tenantId, UsageType usageType, int amount = 1);
    Task<bool> TryReserveUsageAsync(Guid tenantId, UsageType usageType, int amount = 1);
    Task RefundUsageAsync(Guid tenantId, UsageType usageType, int amount = 1);
    
    // Promotion codes
    Task<Upkilo.Core.Entities.PromoCode?> ValidatePromoCodeAsync(string code, Guid tenantId);
    Task<Upkilo.Core.Entities.PromoRedemption?> RedeemPromoCodeAsync(string code, Guid tenantId);
    
    // Billing
    Task<decimal> CalculateProratedAmountAsync(Guid tenantId, Guid newPlanId);
    Task SyncWithStripeAsync(Guid tenantId);
    Task<string> GetPortalSessionUrlAsync(Guid tenantId, string returnUrl);
    
    // Expansion billing
    Task<SubscriptionResult> AddExtraStaffAsync(Guid tenantId, int count);
    Task<SubscriptionResult> AddExtraLocationAsync(Guid tenantId, int count);
    
    // AI Governance
    Task<SubscriptionResult> UpdateAiBudgetAsync(Guid tenantId, decimal budget);
    Task ReportUsageAsync(Guid tenantId, string stripePriceId, long quantity);

    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid tenantId, string planId, bool isAnnual, string? promoCode = null);
    Task<string> CreateBillingPortalSessionAsync(Guid tenantId, string returnUrl);
}

public class CheckoutSessionResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? SessionUrl { get; set; }
}

public class SubscriptionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StripeCheckoutUrl { get; set; }
    public Subscription? Subscription { get; set; }
}

public class UsageSummary
{
    public int BookingsUsed { get; set; }
    public int BookingsLimit { get; set; }
    public int SmsUsed { get; set; }
    public int SmsLimit { get; set; }
    public int AiCreditsUsed { get; set; }
    public int AiCreditsLimit { get; set; }
    public long StorageUsedBytes { get; set; }
    public long StorageLimitBytes { get; set; }
    public int StaffCount { get; set; }
    public int StaffLimit { get; set; }
    public int LocationCount { get; set; }
    public int LocationLimit { get; set; }
    public decimal AiCostUsed { get; set; }
    public decimal AiCostLimit { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Dictionary<string, bool> EnabledFeatures { get; set; } = new();
}

public enum UsageType
{
    Bookings,
    Sms,
    AiCredits,
    Storage,
    ApiCalls
}
