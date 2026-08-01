using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;

namespace Upkilo.API.Middleware;

/// <summary>
/// Middleware to add rate limit headers to responses
/// </summary>
public class RateLimitHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture rate limit metadata before proceeding
        var rateLimitingFeature = context.Features.Get<IRateLimitMetadata>();

        await _next(context);

        // Add standard rate limit headers
        if (rateLimitingFeature != null)
        {
            // These are custom headers we'll set based on our rate limiter configuration
            // In production, integrate with the actual rate limiter's lease info
        }

        // If rate limited, ensure Retry-After header is set
        if (context.Response.StatusCode == 429)
        {
            if (!context.Response.Headers.ContainsKey("Retry-After"))
            {
                context.Response.Headers.Append("Retry-After", "60"); // Default 60 seconds
            }
        }
    }
}

/// <summary>
/// Rate limit response with headers
/// </summary>
public class RateLimitHeadersResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitConfiguration _config;

    public RateLimitHeadersResponseMiddleware(RequestDelegate next, RateLimitConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Track request timing for rate limiting
        var requestStart = DateTimeOffset.UtcNow;

        // Get user identifier for rate limiting
        var userId = context.User?.FindFirst("sub")?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        // Get current usage from the rate limiter
        var usage = _config.GetUsage(userId);

        await _next(context);

        // Add rate limit headers to all API responses
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var resetTime = requestStart.AddSeconds(_config.WindowSeconds);

            context.Response.Headers.Append("X-RateLimit-Limit", _config.RequestsPerWindow.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, _config.RequestsPerWindow - usage - 1).ToString());
            context.Response.Headers.Append("X-RateLimit-Reset", resetTime.ToUnixTimeSeconds().ToString());

            // If rate limited
            if (context.Response.StatusCode == 429)
            {
                var retryAfter = (resetTime - DateTimeOffset.UtcNow).TotalSeconds;
                context.Response.Headers.Append("Retry-After", Math.Max(1, (int)retryAfter).ToString());
            }
        }
    }
}

/// <summary>
/// Rate limit configuration with usage tracking
/// </summary>
public class RateLimitConfiguration
{
    public int RequestsPerWindow { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;

    private readonly Dictionary<string, (int count, DateTimeOffset windowStart)> _usage = new();
    private readonly object _lock = new();

    public int GetUsage(string userId)
    {
        lock (_lock)
        {
            if (_usage.TryGetValue(userId, out var entry))
            {
                if (entry.windowStart.AddSeconds(WindowSeconds) > DateTimeOffset.UtcNow)
                {
                    _usage[userId] = (entry.count + 1, entry.windowStart);
                    return entry.count + 1;
                }
            }

            _usage[userId] = (1, DateTimeOffset.UtcNow);
            return 1;
        }
    }
}

/// <summary>
/// Interface for rate limit metadata
/// </summary>
public interface IRateLimitMetadata
{
    int Limit { get; }
    int Remaining { get; }
    DateTimeOffset ResetAt { get; }
}

/// <summary>
/// Extension methods
/// </summary>
public static class RateLimitHeadersExtensions
{
    public static IServiceCollection AddRateLimitHeaders(this IServiceCollection services, Action<RateLimitConfiguration>? configure = null)
    {
        var config = new RateLimitConfiguration();
        configure?.Invoke(config);
        services.AddSingleton(config);
        return services;
    }

    public static IApplicationBuilder UseRateLimitHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitHeadersResponseMiddleware>();
    }
}
