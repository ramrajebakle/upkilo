using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Middleware;

/// <summary>
/// Middleware to enforce subscription limits and feature access
/// </summary>
public class SubscriptionEnforcerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionEnforcerMiddleware> _logger;

    public SubscriptionEnforcerMiddleware(RequestDelegate next, ILogger<SubscriptionEnforcerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISubscriptionService subscriptionService,
        ITenantProvider tenantProvider,
        Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
    {
        // Resolve tenant ID
        var tenantId = tenantProvider.GetTenantId();

        if (tenantId.HasValue)
        {
            // Store tier for rate limiting / analytics, read from Redis first
            var cacheKey = $"tenant_tier:{tenantId.Value}";
            var cachedTier = await cache.GetStringAsync(cacheKey);

            Upkilo.Core.Entities.SubscriptionTier tier;

            if (!string.IsNullOrEmpty(cachedTier) && Enum.TryParse(cachedTier, out tier))
            {
                context.Items["TenantTier"] = tier;
            }
            else
            {
                var subscription = await subscriptionService.GetSubscriptionAsync(tenantId.Value);
                var isActiveOrTrialing = subscription?.Status is
                    Upkilo.Core.Entities.SubscriptionStatus.Active or
                    Upkilo.Core.Entities.SubscriptionStatus.Trialing;
                var planName = isActiveOrTrialing ? (subscription?.PricingPlan?.Name ?? string.Empty) : string.Empty;
                tier = Enum.TryParse<Upkilo.Core.Entities.SubscriptionTier>(planName, out var parsed)
                    ? parsed
                    : Upkilo.Core.Entities.SubscriptionTier.Free;

                await cache.SetStringAsync(cacheKey, tier.ToString(), new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });

                context.Items["TenantTier"] = tier;
            }
        }

        // Skip for non-authenticated requests or non-API routes
        if (context.User.Identity?.IsAuthenticated != true || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (!tenantId.HasValue)
        {
            await _next(context);
            return;
        }

        // Check if the endpoint requires a specific feature
        var endpoint = context.GetEndpoint();
        var featureAttribute = endpoint?.Metadata.GetMetadata<RequiresFeatureAttribute>();

        if (featureAttribute != null)
        {
            var hasAccess = await subscriptionService.CheckFeatureAccessAsync(tenantId.Value, featureAttribute.FeatureName);
            if (!hasAccess)
            {
                _logger.LogWarning("Access denied to feature {Feature} for tenant {TenantId}",
                    featureAttribute.FeatureName, tenantId);

                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Feature not available",
                    message = $"Your current plan does not include access to {featureAttribute.FeatureName}. Please upgrade to access this feature.",
                    upgradeUrl = "/settings/subscription"
                });
                return;
            }
        }

        // Check and RESERVE usage limits atomically for specific operations
        var usageAttribute = endpoint?.Metadata.GetMetadata<ChecksUsageAttribute>();
        bool reservedQuota = false;

        if (usageAttribute != null && context.Request.Method != "GET")
        {
            reservedQuota = await subscriptionService.TryReserveUsageAsync(
                tenantId.Value, usageAttribute.UsageType, usageAttribute.Amount);

            if (!reservedQuota)
            {
                var usage = await subscriptionService.GetUsageAsync(tenantId.Value);
                _logger.LogWarning("Usage limit exceeded for {UsageType} for tenant {TenantId}",
                    usageAttribute.UsageType, tenantId);

                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Usage limit exceeded",
                    message = $"You have reached your {usageAttribute.UsageType} limit for this billing period.",
                    usage = new
                    {
                        used = GetUsageValue(usage, usageAttribute.UsageType),
                        limit = GetLimitValue(usage, usageAttribute.UsageType),
                        periodEnd = usage.PeriodEnd
                    },
                    upgradeUrl = "/settings/subscription"
                });
                return;
            }
        }

        try
        {
            await _next(context);

            // If the request failed, refund the quota
            if (reservedQuota && (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300))
            {
                await subscriptionService.RefundUsageAsync(tenantId.Value, usageAttribute!.UsageType, usageAttribute.Amount);
            }
        }
        catch (Exception)
        {
            // If an unhandled exception occurred, refund the quota
            if (reservedQuota)
            {
                await subscriptionService.RefundUsageAsync(tenantId.Value, usageAttribute!.UsageType, usageAttribute.Amount);
            }
            throw;
        }
    }

    private static int GetUsageValue(UsageSummary usage, UsageType type) => type switch
    {
        UsageType.Bookings => usage.BookingsUsed,
        UsageType.Sms => usage.SmsUsed,
        UsageType.AiCredits => usage.AiCreditsUsed,
        _ => 0
    };

    private static int GetLimitValue(UsageSummary usage, UsageType type) => type switch
    {
        UsageType.Bookings => usage.BookingsLimit,
        UsageType.Sms => usage.SmsLimit,
        UsageType.AiCredits => usage.AiCreditsLimit,
        _ => 0
    };
}

/// <summary>
/// Attribute to mark endpoints that require a specific subscription feature
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresFeatureAttribute : Attribute
{
    public string FeatureName { get; }

    public RequiresFeatureAttribute(string featureName)
    {
        FeatureName = featureName;
    }
}

/// <summary>
/// Attribute to mark endpoints that consume usage quota
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ChecksUsageAttribute : Attribute
{
    public UsageType UsageType { get; }
    public int Amount { get; }

    public ChecksUsageAttribute(UsageType usageType, int amount = 1)
    {
        UsageType = usageType;
        Amount = amount;
    }
}

/// <summary>
/// Extension methods for SubscriptionEnforcer middleware
/// </summary>
public static class SubscriptionEnforcerExtensions
{
    public static IApplicationBuilder UseSubscriptionEnforcer(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SubscriptionEnforcerMiddleware>();
    }
}
