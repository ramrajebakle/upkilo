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

    private static readonly Dictionary<string, int> TierLimits = new()
    {
        { "Free", 100 },
        { "Starter", 300 },
        { "Professional", 600 },
        { "Enterprise", 1500 }
    };

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

        // Normalize: map 'Business' tier to 'Professional' rate limits (Business is a legacy tier)
        if (tierName == "Business") tierName = "Professional";

        var limitPerMin = TierLimits.GetValueOrDefault(tierName, 100);

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
