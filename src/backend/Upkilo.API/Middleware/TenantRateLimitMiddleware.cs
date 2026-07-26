using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Middleware;

/// <summary>
/// Day 87: Per-tenant API rate limiting using Redis sliding window counters.
/// Limits are tier-based: Starter=1000/day, Growth=5000/day, Business=10000/day, Enterprise=unlimited.
/// Also enforces per-minute burst limits to prevent abuse.
/// </summary>
public class TenantRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRateLimitMiddleware> _logger;
    private readonly IConnectionMultiplexer? _redis;

    private static readonly Dictionary<SubscriptionTier, (int PerDay, int PerMinute)> Limits = new()
    {
        [SubscriptionTier.Free]         = (200, 10),
        [SubscriptionTier.Starter]      = (1000, 30),
        [SubscriptionTier.Professional] = (5000, 60),
        [SubscriptionTier.Business]     = (10000, 120),
        [SubscriptionTier.Enterprise]   = (0, 0), // unlimited
    };

    public TenantRateLimitMiddleware(RequestDelegate next, ILogger<TenantRateLimitMiddleware> logger, IConnectionMultiplexer? redis = null)
    {
        _next = next;
        _logger = logger;
        _redis = redis;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for public/anonymous endpoints and non-API paths
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip if Redis unavailable
        if (_redis == null || !_redis.IsConnected)
        {
            await _next(context);
            return;
        }

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        var tenantId = tenantProvider?.GetTenantId();

        if (tenantId == null)
        {
            await _next(context);
            return;
        }

        var db_context = context.RequestServices.GetService<Upkilo.Infrastructure.Data.AppDbContext>();
        if (db_context == null)
        {
            await _next(context);
            return;
        }

        var tenant = await db_context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null)
        {
            await _next(context);
            return;
        }

        var tier = tenant.SubscriptionTier;

        if (!Limits.TryGetValue(tier, out var limits) || limits.PerDay == 0)
        {
            // Enterprise or unknown tier — no rate limit
            await _next(context);
            return;
        }

        var redisDb = _redis.GetDatabase();
        var now = DateTime.UtcNow;

        // Daily sliding window
        var dayKey = $"rl:day:{tenantId}:{now:yyyyMMdd}";
        var dailyCount = await redisDb.StringIncrementAsync(dayKey);
        if (dailyCount == 1)
            await redisDb.KeyExpireAsync(dayKey, TimeSpan.FromDays(2));

        if (dailyCount > limits.PerDay)
        {
            _logger.LogWarning("[RateLimit] Tenant {TenantId} ({Tier}) exceeded daily limit {Limit}", tenantId, tier, limits.PerDay);
            await WriteRateLimitResponse(context, "Daily API limit exceeded.", limits.PerDay, 0,
                TimeSpan.FromHours(24 - now.Hour));
            return;
        }

        // Per-minute burst window
        var minKey = $"rl:min:{tenantId}:{now:yyyyMMddHHmm}";
        var minCount = await redisDb.StringIncrementAsync(minKey);
        if (minCount == 1)
            await redisDb.KeyExpireAsync(minKey, TimeSpan.FromMinutes(2));

        if (minCount > limits.PerMinute)
        {
            _logger.LogWarning("[RateLimit] Tenant {TenantId} exceeded per-minute burst {Limit}", tenantId, limits.PerMinute);
            await WriteRateLimitResponse(context, "Rate limit exceeded. Please slow down.", limits.PerMinute, 0,
                TimeSpan.FromSeconds(60 - now.Second));
            return;
        }

        // Set rate limit headers
        context.Response.Headers["X-RateLimit-Limit-Day"] = limits.PerDay.ToString();
        context.Response.Headers["X-RateLimit-Remaining-Day"] = Math.Max(0, limits.PerDay - dailyCount).ToString();
        context.Response.Headers["X-RateLimit-Limit-Minute"] = limits.PerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining-Minute"] = Math.Max(0, limits.PerMinute - minCount).ToString();

        await _next(context);
    }

    private static async Task WriteRateLimitResponse(HttpContext ctx, string message, int limit, int remaining, TimeSpan retryAfter)
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
        ctx.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        ctx.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            message,
            retryAfterSeconds = (int)retryAfter.TotalSeconds
        }));
    }
}

public static class TenantRateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantRateLimit(this IApplicationBuilder app)
        => app.UseMiddleware<TenantRateLimitMiddleware>();
}
