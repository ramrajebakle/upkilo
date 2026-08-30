using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Helpers;

/// <summary>
/// Server-side enforcement for the countable entitlements — staff seats, locations and clients.
///
/// These limits existed in the catalogue and were rendered in the billing UI, but nothing ever
/// refused a create. A Free tenant, entitled to one staff member, could POST /staff fifty times
/// and keep every one of them. The only code that ever looked at max_staff was the DOWNGRADE
/// handler, which deactivates the excess after a plan change — so the limit was enforced when a
/// customer moved DOWN a tier and never when they simply exceeded what they had paid for.
/// max_clients was not even that: nothing anywhere consulted it.
///
/// Enforcement is on CREATE only. Nothing retroactively deactivates a tenant already over their
/// limit — that would take working records away from a customer with no warning, and the
/// downgrade handler already owns the plan-change case.
/// </summary>
public static class SeatLimitGuard
{
    /// <summary>
    /// Returns a 403 result when adding one more would exceed the tenant's effective limit, or
    /// null when the create may proceed.
    ///
    /// A LIMIT OF ZERO IS NOT ALWAYS A REFUSAL. The resolver reports zero both for "this tenant
    /// is entitled to none of this" and for "there is no entitlement data to read", and those
    /// two must not be treated alike:
    ///
    ///  - No subscription row at all, or a plan that never mapped this key, is a DATA problem.
    ///    Registration writes PricingPlanId from `freePricingPlan?.Id`, which is nullable, so a
    ///    tenant created before the pricing catalogue was seeded has a subscription pointing at
    ///    no plan. Refusing there would leave a paying, working business unable to add a single
    ///    client or staff member — an outage caused by our own missing row, and a far worse
    ///    failure than the overage it would prevent. These allow, and log a warning so the data
    ///    gap is visible rather than silently absorbed.
    ///
    ///  - A cancelled, expired, suspended or paused subscription is a DELIBERATE business state.
    ///    That refuses.
    ///
    /// Boolean feature gates keep failing closed; this looser reading applies only to countable
    /// resources, where the tenant is already inside the product and using it.
    /// </summary>
    public static async Task<IActionResult?> CheckAsync(
        IEntitlementService entitlements,
        Guid tenantId,
        string featureKey,
        Func<Task<int>> currentCount,
        string resourceName,
        CancellationToken ct = default,
        ILogger? logger = null)
    {
        var set = await entitlements.GetEffectiveEntitlementsAsync(tenantId, ct);

        if (!set.Features.TryGetValue(featureKey, out var entitlement))
        {
            logger?.LogWarning(
                "Seat limit for '{FeatureKey}' could not be resolved for tenant {TenantId}; allowing create",
                featureKey, tenantId);
            return null;
        }

        // Deliberate non-entitlement — refuse.
        if (entitlement.Source == EntitlementSource.SubscriptionInactive)
        {
            return Refuse(resourceName,
                $"Your subscription is {set.SubscriptionStatus.ToLowerInvariant()}. " +
                $"Reactivate it to add {resourceName.ToLowerInvariant()}s.",
                used: 0, limit: 0);
        }

        // Missing entitlement data — allow, but make the gap visible.
        if (entitlement.Source is EntitlementSource.NoSubscription or EntitlementSource.PlanExcluded)
        {
            logger?.LogWarning(
                "Tenant {TenantId} has no usable '{FeatureKey}' entitlement (source {Source}, plan '{Plan}'); " +
                "allowing {Resource} create rather than blocking on missing billing data",
                tenantId, featureKey, entitlement.Source, set.PlanName, resourceName);
            return null;
        }

        if (entitlement.Limit == EntitlementLimits.Unlimited) return null;

        var used = await currentCount();
        if (used < entitlement.Limit) return null;

        return Refuse(resourceName,
            $"Your plan includes {entitlement.Limit} {resourceName.ToLowerInvariant()}s and you have {used}. " +
            "Upgrade your plan or purchase additional capacity to add more.",
            used, entitlement.Limit);
    }

    private static ObjectResult Refuse(string resourceName, string message, int used, int limit) =>
        new(new
        {
            error = $"{resourceName} limit reached",
            message,
            used,
            limit,
            upgradeUrl = "/settings/billing",
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
}
