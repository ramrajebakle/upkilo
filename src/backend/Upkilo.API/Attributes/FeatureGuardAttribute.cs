using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Attributes;

/// <summary>
/// Controller/action gate for a plan feature. Applied to 20+ controllers, making it the widest
/// entitlement surface in the API.
///
/// It now delegates to <see cref="IEntitlementService"/> instead of resolving for itself. What
/// it used to do wrong, all of which the shared resolver handles:
///
///  1. It read <c>Tenant.PricingPlan</c>, a DENORMALISED copy of the plan that is only written
///     during registration and during SubscriptionService.SyncWithStripeAsync. Neither
///     ChangeSubscriptionAsync nor the Stripe subscription-updated webhook updates it — both
///     write Subscription.PricingPlanId only. So after any plan change this gate kept enforcing
///     the PREVIOUS plan until a sync happened to run: an upgraded customer stayed locked out
///     of what they had just paid for, and a downgraded one kept the features they had dropped.
///
///  2. It never checked subscription status, so a cancelled, expired or suspended tenant passed
///     every one of these gates for as long as the plan row said yes.
///
///  3. It kept a third cache — feature_guard:{tenant}:{key}, 5-minute TTL — that nothing
///     anywhere invalidated. An admin grant or a cancellation took up to five minutes to take
///     effect here regardless of what the rest of the system did.
///
/// The feature key is validated against <see cref="FeatureKeys"/> at request time. Passing a key
/// outside the catalogue is a coding error, and denying loudly is the safe direction for it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FeatureGuardAttribute : ActionFilterAttribute
{
    private readonly string _featureKey;

    public FeatureGuardAttribute(string featureKey)
    {
        _featureKey = featureKey;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var tenantProvider = services.GetService<ITenantProvider>();
        var entitlements = services.GetService<IEntitlementService>();

        if (tenantProvider == null || entitlements == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var tenantId = tenantProvider.GetTenantId();
        if (tenantId == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // HasFeatureAsync logs an error and denies for an unknown key, so a typo surfaces in
        // the logs rather than silently refusing paying customers the way the old
        // PascalCase-vs-snake_case mismatch did.
        if (!await entitlements.HasFeatureAsync(tenantId.Value, _featureKey, context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(new
            {
                error = $"Feature '{_featureKey}' is not included in your current plan.",
                upgradeRequired = true,
                upgradeUrl = "/settings/billing",
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
