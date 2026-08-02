using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Middleware;

/// <summary>
/// Tier-based rate limiting middleware.
/// Enforces different request limits based on subscription tier:
///   Free:         100 requests/min
///   Starter:      300 requests/min
///   Professional: 600 requests/min
///   Enterprise:   1500 requests/min
///
/// Uses sliding window counter stored in Redis.
/// Returns 429 with Retry-After header when limit exceeded.
/// </summary>
public class TierRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TierRateLimitMiddleware> _logger;

    // Requests per minute per tenant, keyed by plan name.
    //
    // ⚠️ Every plan in PricingSeeder MUST appear here. This map previously omitted "Agency"
    // entirely, so the $249/mo tier fell through to the 100/min Free default — a paying
    // customer silently throttled to free-tier limits, with nothing logged. The unknown-plan
    // warning below exists so that failure mode cannot recur silently.
    private static readonly Dictionary<string, int> TierLimits = new()
    {
        { "Free", 100 },
        { "Starter", 300 },
        { "Growth", 900 },
        { "Enterprise", 1500 }
    };

    private const int DefaultLimitPerMinute = 100;

    public TierRateLimitMiddleware(RequestDelegate next, ILogger<TierRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and public endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/health") || path.Contains("/swagger"))
        {
            await _next(context);
            return;
        }

        var tenantId = context.Items["TenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId))
        {
            await _next(context);
            return;
        }

        var rateLimitService = context.RequestServices.GetService<Upkilo.Infrastructure.Services.IRateLimitService>();
        if (rateLimitService == null)
        {
            await _next(context);
            return;
        }

        // Determine tier (default to Free)
        // SubscriptionEnforcerMiddleware sets context.Items["TenantTier"] to the SubscriptionTier enum value
        var tierObj = context.Items["TenantTier"];
        var tierName = tierObj?.ToString() ?? "Free";

        // Legacy names from before the tier consolidation. Kept as a safety net for any
        // subscription row still carrying an old plan name; new data will not hit these.
        tierName = tierName switch
        {
            "Professional" or "Business" or "Agency" => "Growth",
            _ => tierName
        };

        if (!TierLimits.TryGetValue(tierName, out var limitPerMin))
        {
            limitPerMin = DefaultLimitPerMinute;
            // Loud on purpose: an unmapped plan means a paying tenant is being throttled to
            // free-tier limits. This is exactly how the Agency gap went unnoticed.
            _logger.LogWarning(
                "Unmapped plan '{Tier}' in TierRateLimitMiddleware — falling back to {Limit} req/min. Add it to TierLimits.",
                tierName, DefaultLimitPerMinute);
        }

        var isAllowed = await rateLimitService.IsAllowedAsync(
            $"tenant:{tenantId}",
            limitPerMin,
            TimeSpan.FromMinutes(1));

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId} (tier: {Tier})", tenantId, tierName);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                tier = tierName,
                limit = limitPerMin,
                retryAfter = 60
            });
            return;
        }

        // Set rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = limitPerMin.ToString();

        await _next(context);
    }
}
