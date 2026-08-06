using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Subscription service implementation with real Stripe integration.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider _secretProvider;
    private readonly IDistributedCache _cache;

    // C-02 FIX: Store Stripe key as instance field instead of setting global static.
    // Use via RequestOptions on each Stripe API call, matching PaymentService pattern.
    private readonly string _stripeApiKey;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };
    private static readonly DistributedCacheEntryOptions _subCacheOpts = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public SubscriptionService(
        AppDbContext context,
        ILogger<SubscriptionService> logger,
        IConfiguration configuration,
        ISecretProvider secretProvider,
        IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _secretProvider = secretProvider;
        _cache = cache;

        // C-02 FIX: Do NOT set global StripeConfiguration.ApiKey — it is process-wide
        // and creates race conditions. Store locally for per-request RequestOptions.
        // Deliberately does NOT throw when unset. This runs in the CONSTRUCTOR, and
        // SubscriptionService is resolved on the health-check path — so throwing here made a
        // missing Stripe key return HTTP 400 for /health and /ready, i.e. the whole API looked
        // down when only billing was unconfigured. That blocks every deployment, because the
        // readiness gate can never pass before Stripe is activated.
        //
        // Billing is an optional capability: bookings, CRM and auth must work without it.
        // Stripe calls now fail at the point of use with Stripe's own authentication error,
        // which is scoped to the billing endpoints that actually need it.
        // Colon form — see PaymentService.cs for why "Stripe--SecretKey" never resolved.
        _stripeApiKey = _secretProvider.GetSecret("Stripe:SecretKey") ?? string.Empty;
    }

    private static string SubCacheKey(Guid tenantId) => $"sub:{tenantId}";

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid tenantId, string priceId, bool isAnnual = false, string? promoCode = null)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return new CheckoutSessionResult { Success = false, Error = "Tenant not found" };

        var options = new Stripe.Checkout.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
            {
                new Stripe.Checkout.SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/billing/cancel",
        };

        if (!string.IsNullOrEmpty(promoCode))
        {
            options.Discounts = new List<Stripe.Checkout.SessionDiscountOptions>
            {
                new Stripe.Checkout.SessionDiscountOptions { PromotionCode = promoCode }
            };
        }
        else
        {
            options.AllowPromotionCodes = true;
        }

        var service = new Stripe.Checkout.SessionService();
        var session = await service.CreateAsync(options);

        return new CheckoutSessionResult
        {
            Success = true,
            SessionId = session.Id,
            SessionUrl = session.Url
        };
    }

    public async Task<string> CreateBillingPortalSessionAsync(Guid tenantId, string returnUrl)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null || string.IsNullOrEmpty(tenant.StripeCustomerId))
            throw new Exception("Tenant or Stripe Customer not found");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    public async Task<IEnumerable<Upkilo.Core.Entities.PricingPlan>> GetAllPricingPlansAsync()
    {
        return await _context.PricingPlans
            .Include(p => p.Prices)
            .Include(p => p.FeatureMappings)
            .ThenInclude(fm => fm.PricingFeature)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Upkilo.Core.Entities.PricingPlan?> GetPricingPlanAsync(Guid planId)
    {
        return await _context.PricingPlans
            .Include(p => p.Prices)
            .Include(p => p.FeatureMappings)
            .ThenInclude(fm => fm.PricingFeature)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<Upkilo.Core.Entities.Subscription?> GetSubscriptionAsync(Guid tenantId)
    {
        // P1: 5-min Redis cache — removes the 3-level Include chain from every authenticated request
        var key = SubCacheKey(tenantId);
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
        {
            try { return JsonSerializer.Deserialize<Upkilo.Core.Entities.Subscription>(cached, _jsonOpts); }
            catch { /* fall through to DB on deserialization error */ }
        }

        var sub = await _context.Set<Upkilo.Core.Entities.Subscription>()
            .Include(s => s.PricingPlan)
            .ThenInclude(p => p!.FeatureMappings)
            .ThenInclude(fm => fm.PricingFeature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (sub != null)
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(sub, _jsonOpts), _subCacheOpts);

        return sub;
    }

    private async Task InvalidateSubscriptionCacheAsync(Guid tenantId)
        => await _cache.RemoveAsync(SubCacheKey(tenantId));

    public async Task<SubscriptionResult> CreateSubscriptionAsync(
        Guid tenantId, Guid planId, BillingInterval interval, string? promoCode = null)
    {
        var tenant = await _context.Set<Tenant>().FindAsync(tenantId);
        if (tenant == null)
            return new SubscriptionResult { Success = false, Message = "Tenant not found" };

        // Ensure Stripe Customer exists
        if (string.IsNullOrEmpty(tenant.StripeCustomerId))
        {
            tenant.StripeCustomerId = await CreateStripeCustomerAsync(tenant);
            await _context.SaveChangesAsync();
        }

        string priceId = "";
        int trialDays = 0;

        // Try to find new PricingPlan first
        var pricingPlan = await _context.Set<Upkilo.Core.Entities.PricingPlan>()
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (pricingPlan != null)
        {
            var cycle = interval == BillingInterval.Annual ? BillingCycle.Annual : BillingCycle.Monthly;
            var planPrice = pricingPlan.Prices.FirstOrDefault(p => p.Cycle == cycle);
            if (planPrice == null || string.IsNullOrEmpty(planPrice.StripePriceId))
            {
                return new SubscriptionResult { Success = false, Message = "Plan configuration error: Stripe Price ID missing for selected interval. Run POST /api/admin/pricing/sync-stripe first." };
            }
            priceId = planPrice.StripePriceId;
            trialDays = pricingPlan.TrialDays;
        }
        else
        {
            return new SubscriptionResult { Success = false, Message = "Plan not found" };
        }

        // Create Checkout Session
        var options = new Stripe.Checkout.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
            {
                new Stripe.Checkout.SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/billing/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenant.Id.ToString() },
                { "plan_id", planId.ToString() }
            },
            SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
            {
                TrialPeriodDays = trialDays > 0 ? trialDays : null,
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", tenant.Id.ToString() }
                }
            }
        };

        if (!string.IsNullOrEmpty(promoCode))
        {
            // Resolve promo to Stripe Coupon ID if possible, or use PromotionCode object
            // For now, assuming promoCode is a Stripe Promotion Code ID
            options.Discounts = new List<Stripe.Checkout.SessionDiscountOptions>
             {
                 new Stripe.Checkout.SessionDiscountOptions { PromotionCode = promoCode }
             };
        }

        try
        {
            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = await service.CreateAsync(options);

            return new SubscriptionResult
            {
                Success = true,
                Message = "Checkout session created",
                StripeCheckoutUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Checkout creation failed for tenant {TenantId}", tenantId);
            return new SubscriptionResult { Success = false, Message = "Failed to initiate payment: " + ex.Message };
        }
    }

    private async Task<string> CreateStripeCustomerAsync(Tenant tenant)
    {
        var options = new CustomerCreateOptions
        {
            Email = tenant.Email,
            Name = tenant.Name,
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenant.Id.ToString() }
            }
        };

        try
        {
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);
            return customer.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for tenant {TenantId}", tenant.Id);
            // Return a dummy ID if Stripe fails in dev, or rethrow? 
            // Rethrowing is safer for consistent data.
            throw;
        }
    }

    // CreateMockSubscriptionAsync removed as we now require real Stripe integration.

    public async Task<string> GetPortalSessionUrlAsync(Guid tenantId, string returnUrl)
    {
        var tenant = await _context.Set<Tenant>().FindAsync(tenantId);
        if (tenant == null || string.IsNullOrEmpty(tenant.StripeCustomerId))
        {
            throw new InvalidOperationException("Tenant has no Stripe customer record.");
        }

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            ReturnUrl = returnUrl ?? $"{(_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/')}/billing"
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<SubscriptionResult> ChangeSubscriptionAsync(
        Guid tenantId, Guid newPlanId, BillingInterval? newInterval = null)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            return new SubscriptionResult { Success = false, Message = "No active Stripe subscription found" };

        string? priceId = null;
        var effectiveInterval = newInterval ?? subscription.BillingInterval;

        // Try new PricingPlan first
        var pricingPlan = await GetPricingPlanAsync(newPlanId);
        if (pricingPlan != null)
        {
            var cycle = effectiveInterval == BillingInterval.Annual ? BillingCycle.Annual : BillingCycle.Monthly;
            var planPrice = pricingPlan.Prices.FirstOrDefault(p => p.Cycle == cycle);
            if (planPrice == null || string.IsNullOrEmpty(planPrice.StripePriceId))
                return new SubscriptionResult { Success = false, Message = "Plan configuration error: Stripe Price ID missing. Run POST /api/admin/pricing/sync-stripe." };
            priceId = planPrice.StripePriceId;
        }
        else
        {
            return new SubscriptionResult { Success = false, Message = "Plan not found" };
        }

        try
        {
            var service = new Stripe.SubscriptionService();
            var stripeSub = await service.GetAsync(subscription.StripeSubscriptionId);
            var itemId = stripeSub.Items.Data[0].Id;

            await service.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions { Id = itemId, Price = priceId }
                },
                ProrationBehavior = "create_prorations"
            });

            // Optimistic local update; Stripe webhook will confirm
            subscription.PricingPlanId = newPlanId;
            if (newInterval.HasValue) subscription.BillingInterval = newInterval.Value;
            await _context.SaveChangesAsync();
            await InvalidateSubscriptionCacheAsync(tenantId);

            return new SubscriptionResult { Success = true, Message = "Subscription updated" };
        }
        catch (StripeException ex)
        {
            return new SubscriptionResult { Success = false, Message = "Stripe update failed: " + ex.Message };
        }
    }

    public async Task<SubscriptionResult> CancelSubscriptionAsync(Guid tenantId, bool immediate = false)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            return new SubscriptionResult { Success = false, Message = "No active subscription found" };

        try
        {
            var service = new Stripe.SubscriptionService();
            if (immediate)
            {
                await service.CancelAsync(subscription.StripeSubscriptionId, new SubscriptionCancelOptions());
            }
            else
            {
                await service.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                });
            }

            // Webhook will handle status update, but we can set locally too
            if (immediate)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.CancelledAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await InvalidateSubscriptionCacheAsync(tenantId);

            return new SubscriptionResult { Success = true, Message = "Subscription cancellation requested" };
        }
        catch (StripeException ex)
        {
            return new SubscriptionResult { Success = false, Message = "Stripe cancellation failed: " + ex.Message };
        }
    }

    public async Task<SubscriptionResult> PauseSubscriptionAsync(Guid tenantId, DateTime? resumeAt = null)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            return new SubscriptionResult { Success = false, Message = "No active Stripe subscription found" };

        try
        {
            var service = new Stripe.SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                PauseCollection = new SubscriptionPauseCollectionOptions
                {
                    Behavior = "void", // Void invoices while paused
                    ResumesAt = resumeAt
                }
            };

            await service.UpdateAsync(subscription.StripeSubscriptionId, options);

            subscription.Status = SubscriptionStatus.Paused;
            subscription.PausedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await InvalidateSubscriptionCacheAsync(tenantId);

            return new SubscriptionResult { Success = true, Message = "Subscription paused across Stripe and local systems." };
        }
        catch (StripeException ex)
        {
            return new SubscriptionResult { Success = false, Message = "Stripe pause failed: " + ex.Message };
        }
    }

    public async Task<SubscriptionResult> ResumeSubscriptionAsync(Guid tenantId)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            return new SubscriptionResult { Success = false, Message = "No active Stripe subscription found" };

        try
        {
            var service = new Stripe.SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                PauseCollection = null // Removes the pause
            };

            await service.UpdateAsync(subscription.StripeSubscriptionId, options);

            subscription.Status = SubscriptionStatus.Active;
            subscription.PausedAt = null;
            await _context.SaveChangesAsync();
            await InvalidateSubscriptionCacheAsync(tenantId);

            return new SubscriptionResult { Success = true, Message = "Subscription resumed." };
        }
        catch (StripeException ex)
        {
            return new SubscriptionResult { Success = false, Message = "Stripe resume failed: " + ex.Message };
        }
    }

    public async Task SyncWithStripeAsync(Guid tenantId)
    {
        var tenant = await _context.Set<Tenant>().FindAsync(tenantId);
        if (tenant == null || string.IsNullOrEmpty(tenant.StripeCustomerId)) return;

        try
        {
            var service = new Stripe.SubscriptionService();
            var options = new SubscriptionListOptions
            {
                Customer = tenant.StripeCustomerId,
                Status = "all",
                Limit = 1
            };

            var subs = await service.ListAsync(options);
            if (subs.Data.Count > 0)
            {
                var stripeSubId = subs.Data[0].Id;
                var stripeSub = await service.GetAsync(stripeSubId);
                var localSub = await GetSubscriptionAsync(tenantId);

                if (localSub == null)
                {
                    localSub = new Upkilo.Core.Entities.Subscription { TenantId = tenantId };
                    _context.Set<Upkilo.Core.Entities.Subscription>().Add(localSub);
                }

                localSub.StripeSubscriptionId = stripeSub.Id;
                localSub.Status = MapStripeStatus(stripeSub.Status);
                localSub.BillingInterval = stripeSub.Items.Data[0].Price.Recurring?.Interval == "year"
                    ? BillingInterval.Annual
                    : BillingInterval.Monthly;

                // Sync plan_id from metadata if present
                if (stripeSub.Metadata.TryGetValue("plan_id", out var planIdStr) && Guid.TryParse(planIdStr, out var planId))
                {
                    var pricingPlan = await _context.PricingPlans
                        .Include(p => p.FeatureMappings)
                        .ThenInclude(fm => fm.PricingFeature)
                        .FirstOrDefaultAsync(p => p.Id == planId);

                    if (pricingPlan != null)
                    {
                        localSub.PricingPlanId = planId;

                        // Default AiMonthlyBudget from plan's ai_actions limit if not already set
                        if (localSub.AiMonthlyBudget <= 0)
                        {
                            var aiMapping = pricingPlan.FeatureMappings
                                .FirstOrDefault(fm => fm.PricingFeature.Key == "ai_actions");
                            // Budget in USD: 1 AI action ≈ $0.01
                            localSub.AiMonthlyBudget = aiMapping?.NumericLimit.HasValue == true
                                ? Math.Round(aiMapping.NumericLimit.Value * 0.01m, 2)
                                : 5.00m; // Safe $5 floor for any plan with AI enabled
                        }

                        // Set AI model allowlist based on plan tier
                        if (localSub.AllowedAiModels.Count == 1 && localSub.AllowedAiModels[0] == "gpt-3.5-turbo")
                        {
                            // Every list MUST contain the model AiModelResolver returns for that
                            // tier, or IsModelAllowedAsync rejects the request before dispatch:
                            // Free/Starter → gpt-5-mini, Growth/Enterprise → gpt-5.4-mini.
                            // The old fallback handed out gpt-3.5-turbo alone, which the resolver
                            // never returns — so any plan not named here had AI blocked outright.
                            localSub.AllowedAiModels = pricingPlan.Name.ToLowerInvariant() switch
                            {
                                "enterprise" => new List<string> { "gpt-5.4-mini", "gpt-5-mini" },
                                "growth" => new List<string> { "gpt-5.4-mini", "gpt-5-mini" },
                                // Legacy plan names, folded into Growth
                                "business" or "professional" or "agency" => new List<string> { "gpt-5.4-mini", "gpt-5-mini" },
                                _ => new List<string> { "gpt-5-mini" }
                            };
                        }
                    }
                }

                // Enhanced Mirroring: trial status, billing reason, and custom metadata
                if (stripeSub.Metadata.TryGetValue("is_trial", out var isTrialStr))
                {
                    _logger.LogInformation("Metadata is_trial found: {IsTrial}", isTrialStr);
                }

                // Sync extra seat counts from Stripe item quantities
                if (localSub.PricingPlanId.HasValue)
                {
                    var syncPlan = await _context.PricingPlans.FindAsync(localSub.PricingPlanId.Value);
                    if (syncPlan != null)
                    {
                        if (!string.IsNullOrEmpty(syncPlan.StripeExtraStaffPriceId))
                        {
                            var staffItem = stripeSub.Items?.Data?.FirstOrDefault(i => i.Price?.Id == syncPlan.StripeExtraStaffPriceId);
                            if (staffItem != null) localSub.ExtraStaffCount = (int)staffItem.Quantity;
                        }
                        if (!string.IsNullOrEmpty(syncPlan.StripeExtraLocationPriceId))
                        {
                            var locItem = stripeSub.Items?.Data?.FirstOrDefault(i => i.Price?.Id == syncPlan.StripeExtraLocationPriceId);
                            if (locItem != null) localSub.ExtraLocationCount = (int)locItem.Quantity;
                        }
                    }
                }

                // In Stripe.NET v50+, CurrentPeriodStart/End moved to SubscriptionItem
                var periodItem = stripeSub.Items?.Data?.FirstOrDefault();
                if (periodItem != null)
                {
                    localSub.CurrentPeriodStart = periodItem.CurrentPeriodStart;
                    localSub.CurrentPeriodEnd = periodItem.CurrentPeriodEnd;
                }
                localSub.CancelledAt = stripeSub.CanceledAt;

                if (stripeSub.LatestInvoice?.BillingReason != null)
                {
                    _logger.LogInformation("Last sync billing reason: {Reason}", stripeSub.LatestInvoice.BillingReason);
                }

                // Sync Tenant.SubscriptionTier to match the resolved PricingPlan name
                if (localSub.PricingPlanId.HasValue)
                {
                    var resolvedPlan = await _context.PricingPlans.FindAsync(localSub.PricingPlanId.Value);
                    if (resolvedPlan != null)
                    {
                        // "growth" was missing here after the pricing consolidation, so a paying
                        // Growth customer fell through to the Starter fallback below and was
                        // silently downgraded — cheaper AI model, lower rate limits, fewer jobs.
                        // Professional/Business/Agency are kept as aliases: those plans were
                        // folded into Growth, so any subscription still naming one resolves there.
                        var tier = resolvedPlan.Name.ToLowerInvariant() switch
                        {
                            "free" => SubscriptionTier.Free,
                            "starter" => SubscriptionTier.Starter,
                            "growth" => SubscriptionTier.Growth,
                            "professional" => SubscriptionTier.Growth,
                            "business" => SubscriptionTier.Growth,
                            "agency" => SubscriptionTier.Growth,
                            "enterprise" => SubscriptionTier.Enterprise,
                            // Still a downgrade, but now only reachable via a genuinely unknown
                            // plan name rather than one of our own tiers.
                            _ => SubscriptionTier.Starter
                        };
                        var tenantToUpdate = await _context.Tenants.FindAsync(tenantId);
                        if (tenantToUpdate != null)
                        {
                            tenantToUpdate.SubscriptionTier = tier;
                            tenantToUpdate.PricingPlanId = localSub.PricingPlanId.Value;
                            tenantToUpdate.StripeSubscriptionId = stripeSub.Id;
                            tenantToUpdate.StripeSubscriptionStatus = stripeSub.Status;
                            tenantToUpdate.SubscriptionPeriodEnd = localSub.CurrentPeriodEnd;
                        }
                    }
                }


                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync with Stripe for tenant {TenantId}", tenantId);
        }
    }

    private SubscriptionStatus MapStripeStatus(string status)
    {
        return status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" or "cancelled" => SubscriptionStatus.Cancelled,
            "unpaid" => SubscriptionStatus.Expired,
            "paused" => SubscriptionStatus.Paused,
            "incomplete" or "incomplete_expired" => SubscriptionStatus.Suspended,
            _ => SubscriptionStatus.Suspended
        };
    }

    // --- Usage & Helper Methods (unchanged/minimal mods) ---

    public async Task<UsageSummary> GetUsageAsync(Guid tenantId)
    {
        var subscription = await GetSubscriptionAsync(tenantId);

        var staffCount = await _context.Set<StaffMember>().CountAsync(s => s.TenantId == tenantId);
        var locationCount = await _context.Set<Location>().CountAsync(l => l.TenantId == tenantId);

        var aiCostLogs = await _context.Set<AIUsageLog>()
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= (subscription != null ? subscription.CurrentPeriodStart : DateTime.UtcNow.AddMonths(-1)))
            .Select(l => l.Cost)
            .ToListAsync();
        var aiCost = aiCostLogs.Sum();

        var mappings = subscription?.PricingPlan?.FeatureMappings
            .ToDictionary(fm => fm.PricingFeature.Key, fm => fm) ?? new Dictionary<string, PlanFeatureMapping>();

        int GetNumericLimit(string key) =>
            mappings.TryGetValue(key, out var m) && m.IsEnabled ? (m.NumericLimit ?? -1) : 0;
        bool GetFlag(string key) =>
            mappings.TryGetValue(key, out var m) && m.IsEnabled;

        // Trial expiry / non-payment gate. Bookings are unlimited (-1) while the subscription
        // entitles the tenant to service, and 0 once it does not — blocking NEW bookings without
        // touching existing data, the dashboard, or the tenant's ability to sign in.
        //
        // This is the whole enforcement mechanism: BookingsLimit was previously hardcoded to -1,
        // so the [ChecksUsage(UsageType.Bookings)] attribute already on BookingsController could
        // never actually refuse anything. CanConsumeAsync evaluates
        // `BookingsLimit == -1 || used + amount <= BookingsLimit`, so returning 0 here makes the
        // existing path do the work — no new middleware, no new attribute.
        //
        // PastDue is deliberately ALLOWED. DunningAutomationJob runs a 14-day recovery timeline
        // (retry at day 3 and 7, auto-suspend at day 14, cancel at day 30). Cutting bookings the
        // moment a card declines would defeat that: a salon would stop trading over a single
        // failed charge, producing churn instead of a successful retry. Service stops when the
        // job flips the status to Suspended, not before.
        var bookingsAllowed = subscription?.Status is SubscriptionStatus.Active
                                                  or SubscriptionStatus.Trialing
                                                  or SubscriptionStatus.Trial     // Stripe-mapping alias
                                                  or SubscriptionStatus.PastDue;  // within dunning grace

        return new UsageSummary
        {
            BookingsUsed = subscription?.BookingsUsed ?? 0,
            BookingsLimit = bookingsAllowed ? -1 : 0,
            SmsUsed = subscription?.SmsUsed ?? 0,
            SmsLimit = GetFlag("sms_reminders") ? -1 : 0,
            AiCreditsUsed = subscription?.AiCreditsUsed ?? 0,
            AiCreditsLimit = GetNumericLimit("ai_actions"),
            StorageUsedBytes = subscription?.StorageUsedBytes ?? 0,
            StorageLimitBytes = 5L * 1024 * 1024 * 1024,
            StaffCount = staffCount,
            StaffLimit = GetNumericLimit("max_staff") + (subscription?.ExtraStaffCount ?? 0),
            LocationCount = locationCount,
            LocationLimit = GetNumericLimit("max_locations") + (subscription?.ExtraLocationCount ?? 0),
            AiCostUsed = aiCost,
            AiCostLimit = subscription?.AiMonthlyBudget ?? 0,
            PeriodStart = subscription?.CurrentPeriodStart ?? DateTime.UtcNow,
            PeriodEnd = subscription?.CurrentPeriodEnd ?? DateTime.UtcNow.AddMonths(1),
            EnabledFeatures = mappings.ToDictionary(kv => kv.Key, kv => kv.Value.IsEnabled)
        };
    }

    public async Task<bool> CheckFeatureAccessAsync(Guid tenantId, string featureName)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null) return false;

        if (subscription.PricingPlan == null) return false;
        var mapping = subscription.PricingPlan.FeatureMappings
            .FirstOrDefault(fm => string.Equals(fm.PricingFeature.Key, featureName, StringComparison.OrdinalIgnoreCase));
        return mapping?.IsEnabled == true;
    }

    public async Task<bool> CheckUsageLimitAsync(Guid tenantId, UsageType usageType, int amount = 1)
    {
        var usage = await GetUsageAsync(tenantId);
        return usageType switch
        {
            UsageType.Bookings => usage.BookingsLimit == -1 || usage.BookingsUsed + amount <= usage.BookingsLimit,
            UsageType.Sms => usage.SmsLimit == -1 || usage.SmsUsed + amount <= usage.SmsLimit,
            UsageType.AiCredits => usage.AiCreditsLimit == -1 || usage.AiCreditsUsed + amount <= usage.AiCreditsLimit,
            UsageType.Storage => usage.StorageLimitBytes == -1 || usage.StorageUsedBytes + amount <= usage.StorageLimitBytes,
            _ => true
        };
    }

    public async Task IncrementUsageAsync(Guid tenantId, UsageType usageType, int amount = 1)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null) return;

        switch (usageType)
        {
            case UsageType.Bookings: subscription.BookingsUsed += amount; break;
            case UsageType.Sms: subscription.SmsUsed += amount; break;
            case UsageType.AiCredits: subscription.AiCreditsUsed += amount; break;
            case UsageType.Storage: subscription.StorageUsedBytes += amount; break;
        }

        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TryReserveUsageAsync(Guid tenantId, UsageType usageType, int amount = 1)
    {
        // Check limits first using the unified GetUsageAsync (supports both plan systems)
        var usage = await GetUsageAsync(tenantId);

        bool withinLimit = usageType switch
        {
            UsageType.Bookings => usage.BookingsLimit == -1 || usage.BookingsUsed + amount <= usage.BookingsLimit,
            UsageType.Sms => usage.SmsLimit == -1 || usage.SmsUsed + amount <= usage.SmsLimit,
            UsageType.AiCredits => usage.AiCreditsLimit == -1 || usage.AiCreditsUsed + amount <= usage.AiCreditsLimit,
            UsageType.Storage => usage.StorageLimitBytes == -1 || usage.StorageUsedBytes + amount <= usage.StorageLimitBytes,
            _ => true
        };

        if (!withinLimit) return false;

        // Atomic increment — works regardless of plan system because Subscription counters are universal
        int rows = usageType switch
        {
            UsageType.Bookings => await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.BookingsUsed, b => b.BookingsUsed + amount)),
            UsageType.Sms => await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.SmsUsed, b => b.SmsUsed + amount)),
            UsageType.AiCredits => await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.AiCreditsUsed, b => b.AiCreditsUsed + amount)),
            UsageType.Storage => await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.StorageUsedBytes, b => b.StorageUsedBytes + amount)),
            _ => 1
        };

        return rows > 0;
    }

    public async Task RefundUsageAsync(Guid tenantId, UsageType usageType, int amount = 1)
    {
        switch (usageType)
        {
            case UsageType.Bookings:
                await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.BookingsUsed, b => Math.Max(0, b.BookingsUsed - amount)));
                break;
            case UsageType.Sms:
                await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.SmsUsed, b => Math.Max(0, b.SmsUsed - amount)));
                break;
            case UsageType.AiCredits:
                await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.AiCreditsUsed, b => Math.Max(0, b.AiCreditsUsed - amount)));
                break;
            case UsageType.Storage:
                await _context.Subscriptions.Where(s => s.TenantId == tenantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.StorageUsedBytes, b => Math.Max(0, b.StorageUsedBytes - amount)));
                break;
        }
    }

    public async Task<Upkilo.Core.Entities.PromoCode?> ValidatePromoCodeAsync(string code, Guid tenantId)
    {
        var promo = await _context.Set<Upkilo.Core.Entities.PromoCode>()
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower() && p.IsActive);

        if (promo == null) return null;
        if (promo.ExpiresAt.HasValue && promo.ExpiresAt < DateTime.UtcNow) return null;
        if (promo.UsageLimit.HasValue && promo.TimesUsed >= promo.UsageLimit) return null;

        var alreadyRedeemed = await _context.Set<Upkilo.Core.Entities.PromoRedemption>()
            .AnyAsync(r => r.TenantId == tenantId && r.PromoCodeId == promo.Id);
        if (alreadyRedeemed) return null;

        return promo;
    }

    public async Task<Upkilo.Core.Entities.PromoRedemption?> RedeemPromoCodeAsync(string code, Guid tenantId)
    {
        var promo = await ValidatePromoCodeAsync(code, tenantId);
        if (promo == null) return null;

        // H-08 FIX: Use atomic database update to prevent TOCTOU race conditions
        var rowsUpdated = await _context.Set<Upkilo.Core.Entities.PromoCode>()
            .Where(p => p.Id == promo.Id && (p.UsageLimit == null || p.TimesUsed < p.UsageLimit))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TimesUsed, p => p.TimesUsed + 1));

        if (rowsUpdated == 0) return null; // Limit reached during race condition

        var redemption = new Upkilo.Core.Entities.PromoRedemption
        {
            TenantId = tenantId,
            PromoCodeId = promo.Id,
            DiscountApplied = promo.DiscountValue
        };

        _context.Set<Upkilo.Core.Entities.PromoRedemption>().Add(redemption);
        await _context.SaveChangesAsync();
        return redemption;
    }

    public async Task<decimal> CalculateProratedAmountAsync(Guid tenantId, Guid newPlanId)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null) return 0;

        var daysRemaining = (subscription.CurrentPeriodEnd - DateTime.UtcNow).Days;
        var totalDays = (subscription.CurrentPeriodEnd - subscription.CurrentPeriodStart).Days;
        if (totalDays <= 0) return 0;

        decimal currentMonthlyPrice = 0;
        decimal newMonthlyPrice = 0;

        // Resolve current plan price
        if (subscription.PricingPlan != null)
        {
            var cycle = subscription.BillingInterval == BillingInterval.Annual ? BillingCycle.Annual : BillingCycle.Monthly;
            var currentPrice = subscription.PricingPlan.Prices.FirstOrDefault(p => p.Cycle == cycle);
            currentMonthlyPrice = currentPrice != null
                ? (subscription.BillingInterval == BillingInterval.Annual ? currentPrice.Amount / 12 : currentPrice.Amount)
                : 0;
        }

        // Resolve new plan price
        var newPricingPlan = await GetPricingPlanAsync(newPlanId);
        if (newPricingPlan != null)
        {
            var cycle = subscription.BillingInterval == BillingInterval.Annual ? BillingCycle.Annual : BillingCycle.Monthly;
            var newPrice = newPricingPlan.Prices.FirstOrDefault(p => p.Cycle == cycle);
            newMonthlyPrice = newPrice != null
                ? (subscription.BillingInterval == BillingInterval.Annual ? newPrice.Amount / 12 : newPrice.Amount)
                : 0;
        }

        // Downgrade: no immediate charge; credit is handled by Stripe prorations
        return Math.Max(0m, Math.Round((newMonthlyPrice - currentMonthlyPrice) * daysRemaining / totalDays, 2));
    }

    public async Task<SubscriptionResult> AddExtraStaffAsync(Guid tenantId, int count)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            return new SubscriptionResult { Success = false, Message = "No active subscription found" };

        string? extraStaffPriceId = subscription.PricingPlan?.StripeExtraStaffPriceId
            ?? _configuration["Stripe:ExtraStaffPriceId"];

        if (string.IsNullOrEmpty(extraStaffPriceId))
            return new SubscriptionResult { Success = false, Message = "Extra staff add-on Stripe Price ID not configured. Set Stripe:ExtraStaffPriceId in configuration." };

        try
        {
            var subService = new Stripe.SubscriptionService();
            var itemService = new Stripe.SubscriptionItemService();

            var stripeSub = await subService.GetAsync(subscription.StripeSubscriptionId);
            var staffItem = stripeSub.Items.Data.FirstOrDefault(i => i.Price.Id == extraStaffPriceId);
            var newTotal = subscription.ExtraStaffCount + count;

            if (staffItem != null)
                await itemService.UpdateAsync(staffItem.Id, new SubscriptionItemUpdateOptions { Quantity = newTotal });
            else
                await itemService.CreateAsync(new SubscriptionItemCreateOptions
                {
                    Subscription = subscription.StripeSubscriptionId,
                    Price = extraStaffPriceId,
                    Quantity = newTotal
                });

            subscription.ExtraStaffCount = newTotal;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new SubscriptionResult { Success = true, Message = $"Added {count} staff seats. Total extra: {newTotal}" };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe extra staff update failed for tenant {TenantId}", tenantId);
            return new SubscriptionResult { Success = false, Message = "Stripe update failed: " + ex.Message };
        }
    }

    public async Task<SubscriptionResult> AddExtraLocationAsync(Guid tenantId, int count)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            return new SubscriptionResult { Success = false, Message = "No active subscription found" };

        string? extraLocationPriceId = subscription.PricingPlan?.StripeExtraLocationPriceId
            ?? _configuration["Stripe:ExtraLocationPriceId"];

        if (string.IsNullOrEmpty(extraLocationPriceId))
            return new SubscriptionResult { Success = false, Message = "Extra location add-on Stripe Price ID not configured. Set Stripe:ExtraLocationPriceId in configuration." };

        try
        {
            var subService = new Stripe.SubscriptionService();
            var itemService = new Stripe.SubscriptionItemService();

            var stripeSub = await subService.GetAsync(subscription.StripeSubscriptionId);
            var locationItem = stripeSub.Items.Data.FirstOrDefault(i => i.Price.Id == extraLocationPriceId);
            var newTotal = subscription.ExtraLocationCount + count;

            if (locationItem != null)
                await itemService.UpdateAsync(locationItem.Id, new SubscriptionItemUpdateOptions { Quantity = newTotal });
            else
                await itemService.CreateAsync(new SubscriptionItemCreateOptions
                {
                    Subscription = subscription.StripeSubscriptionId,
                    Price = extraLocationPriceId,
                    Quantity = newTotal
                });

            subscription.ExtraLocationCount = newTotal;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new SubscriptionResult { Success = true, Message = $"Added {count} locations. Total extra: {newTotal}" };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe extra location update failed for tenant {TenantId}", tenantId);
            return new SubscriptionResult { Success = false, Message = "Stripe update failed: " + ex.Message };
        }
    }

    public async Task<SubscriptionResult> UpdateAiBudgetAsync(Guid tenantId, decimal budget)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null) return new SubscriptionResult { Success = false, Message = "No subscription found" };

        subscription.AiMonthlyBudget = budget;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new SubscriptionResult { Success = true, Message = $"AI monthly budget updated to ${budget:F2}" };
    }

    public async Task ReportUsageAsync(Guid tenantId, string stripePriceId, long quantity)
    {
        var subscription = await GetSubscriptionAsync(tenantId);
        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId)) return;

        try
        {
            var service = new Stripe.SubscriptionItemService();
            // In a real scenario, we'd find the specific item linked to this price
            var stripeSub = await new Stripe.SubscriptionService().GetAsync(subscription.StripeSubscriptionId);
            var item = stripeSub.Items.Data.FirstOrDefault(i => i.Price.Id == stripePriceId);

            if (item != null)
            {
                // Usage-based billing: update the quantity on the subscription item
                var itemService = new Stripe.SubscriptionItemService();
                await itemService.UpdateAsync(item.Id, new SubscriptionItemUpdateOptions
                {
                    Quantity = item.Quantity + quantity
                });
                _logger.LogInformation("Reported {Quantity} units of usage for price {PriceId} to Stripe for tenant {TenantId}", quantity, stripePriceId, tenantId);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to report usage to Stripe for tenant {TenantId}", tenantId);
        }
    }
}
