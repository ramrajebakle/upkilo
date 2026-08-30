namespace Upkilo.Core.Entities;

/// <summary>
/// Maps a <see cref="PricingPlan"/> name onto the <see cref="SubscriptionTier"/> enum stored
/// denormalised on <see cref="Tenant"/>.
///
/// WHY IT IS SHARED
/// ----------------
/// Tenant.SubscriptionTier is a cached copy of "which plan is this tenant on", read by things
/// that are not entitlement checks and legitimately want a coarse tier: AiModelResolver (which
/// model to route to), JobQuotaService (concurrent background jobs), the rate limiter, and the
/// churn/digest jobs.
///
/// The mapping used to live inline inside SubscriptionService.SyncWithStripeAsync, which was
/// also the ONLY place that wrote the column. Every other path that changes a tenant's plan —
/// SubscriptionService.ChangeSubscriptionAsync and the Stripe customer.subscription.updated
/// webhook — wrote Subscription.PricingPlanId and left the tenant copy untouched, so the two
/// disagreed until a sync happened to run. During that window an upgraded customer was still
/// routed to the cheap AI model and the low job quota they had just paid to leave behind.
///
/// Extracting it here means the mapping has one definition and every writer can reach it.
/// </summary>
public static class SubscriptionTierMap
{
    /// <summary>
    /// Resolves a plan name to its tier. Case-insensitive.
    ///
    /// Professional, Business and Agency are retained as aliases for Growth: those tiers were
    /// folded into Growth during the pricing consolidation, and a subscription still naming one
    /// must not fall through to the default.
    ///
    /// The default is deliberately <see cref="SubscriptionTier.Free"/> rather than Starter. An
    /// unrecognised plan name means we do not know what the customer bought, and quietly
    /// granting them a paid tier's AI models and job quota is the wrong direction to guess in —
    /// the previous Starter default did exactly that. Free is visible in support and safe.
    /// </summary>
    public static SubscriptionTier FromPlanName(string? planName) =>
        (planName ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "free" => SubscriptionTier.Free,
            "starter" => SubscriptionTier.Starter,
            "growth" => SubscriptionTier.Growth,
            "professional" => SubscriptionTier.Growth,  // legacy alias
            "business" => SubscriptionTier.Growth,      // legacy alias
            "agency" => SubscriptionTier.Growth,        // legacy alias
            "enterprise" => SubscriptionTier.Enterprise,
            _ => SubscriptionTier.Free,
        };
}
