using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FeatureGuardAttribute : ActionFilterAttribute
{
    private readonly string _featureKey;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public FeatureGuardAttribute(string featureKey)
    {
        _featureKey = featureKey;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenantProvider = context.HttpContext.RequestServices.GetService<ITenantProvider>();
        var dbContext = context.HttpContext.RequestServices.GetService<AppDbContext>();

        if (tenantProvider == null || dbContext == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var tenantId = tenantProvider.GetTenantId();
        if (tenantId == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        bool isEnabled = await IsFeatureEnabledAsync(tenantId.Value, dbContext,
            context.HttpContext.RequestServices.GetService<IDistributedCache>());

        if (!isEnabled)
        {
            context.Result = new ObjectResult(new
            {
                error = $"Feature '{_featureKey}' is not included in your current plan.",
                upgradeRequired = true
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }

    private async Task<bool> IsFeatureEnabledAsync(Guid tenantId, AppDbContext db, IDistributedCache? cache)
    {
        var cacheKey = $"feature_guard:{tenantId}:{_featureKey}";

        if (cache != null)
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached != null)
                return cached == "1";
        }

        // Load tenant with PricingPlan feature mappings
        var tenant = await db.Tenants
            .Include(t => t.PricingPlan)
            .ThenInclude(p => p!.FeatureMappings)
            .ThenInclude(fm => fm.PricingFeature)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        bool result;

        if (tenant?.PricingPlan != null)
        {
            var mapping = tenant.PricingPlan.FeatureMappings
                .FirstOrDefault(fm => fm.PricingFeature.Key == _featureKey);
            result = mapping?.IsEnabled == true;
        }
        else
        {
            // No PricingPlan assigned — deny access to gated features
            result = false;
        }

        if (cache != null)
        {
            await cache.SetStringAsync(cacheKey, result ? "1" : "0",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        }

        return result;
    }
}
