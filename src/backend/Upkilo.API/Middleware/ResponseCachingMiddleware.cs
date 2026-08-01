using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Upkilo.API.Middleware;

/// <summary>
/// Caches GET responses in memory for configurable durations to reduce database load.
/// Only caches 200 OK responses. Cache key is based on path + query + tenant context.
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResponseCachingMiddleware> _logger;
    private readonly HashSet<string> _cacheablePathPrefixes;

    public ResponseCachingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<ResponseCachingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;

        // Paths that benefit from short-lived caching (30s default)
        _cacheablePathPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/v1/services",
            "/api/v1/analytics",
            "/api/v1/notifications",
            "/api/v1/loyalty/stats",
            "/api/v1/memberships/stats",
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        // Check if this path should be cached
        if (!_cacheablePathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Build cache key including tenant context
        var tenantId = context.User?.FindFirst("tenant_id")?.Value ?? "default";
        var cacheKey = BuildCacheKey(tenantId, path, context.Request.QueryString.Value);

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out CachedResponse? cached) && cached != null)
        {
            _logger.LogDebug("Cache HIT for {Path}", path);
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers.Append("X-Cache", "HIT");
            await context.Response.Body.WriteAsync(cached.Body);
            return;
        }

        _logger.LogDebug("Cache MISS for {Path}", path);

        // Capture the response
        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        // Only cache 200 OK responses
        if (context.Response.StatusCode == 200)
        {
            memStream.Seek(0, SeekOrigin.Begin);
            var responseBody = memStream.ToArray();

            var cachedResponse = new CachedResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType ?? "application/json",
                Body = responseBody,
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                .SetSlidingExpiration(TimeSpan.FromSeconds(15))
                .SetSize(responseBody.Length);

            _cache.Set(cacheKey, cachedResponse, cacheOptions);
            context.Response.Headers.Append("X-Cache", "MISS");
        }

        // Copy to original stream
        memStream.Seek(0, SeekOrigin.Begin);
        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }

    private static string BuildCacheKey(string tenantId, string path, string? queryString)
    {
        var raw = $"{tenantId}:{path}:{queryString ?? ""}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"rc:{Convert.ToHexString(hash)[..16]}";
    }

    private class CachedResponse
    {
        public int StatusCode { get; init; }
        public string ContentType { get; init; } = "application/json";
        public byte[] Body { get; init; } = Array.Empty<byte>();
    }
}
