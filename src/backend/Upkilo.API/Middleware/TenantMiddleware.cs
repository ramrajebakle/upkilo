using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.API.Middleware;

/// <summary>
/// Tenant resolution middleware — extracts tenant from subdomain, custom domain, or JWT.
/// WL-04: custom domain lookup is now async and cached via IMemoryCache (5-minute TTL)
///        to eliminate the per-request synchronous DB hit.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private readonly IMemoryCache _cache;

    private const string CustomDomainCachePrefix = "cd:";
    private static readonly TimeSpan CustomDomainCacheTtl = TimeSpan.FromMinutes(5);

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = await ResolveTenantIdAsync(context);

        if (!string.IsNullOrEmpty(tenantId))
        {
            // --- SECURITY HARDENING: CROSS-TENANT CHECK ---
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var jwtTenantId = context.User?.FindFirst("tenant_id")?.Value;
            var userRole = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (isAuthenticated && userRole != "SuperAdmin")
            {
                if (string.IsNullOrEmpty(jwtTenantId) || jwtTenantId != tenantId)
                {
                    _logger.LogCritical(
                        "SECURITY BREACH ATTEMPT: User {UserId} (tenant {JwtTenantId}) tried to access tenant {TargetTenantId}",
                        context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        jwtTenantId ?? "NONE",
                        tenantId);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "SECURITY_VIOLATION", message = "Cross-tenant access detected and blocked." });
                    return;
                }
            }

            context.Items["TenantId"] = tenantId;
            _logger.LogDebug("Tenant resolved: {TenantId}", tenantId);
        }

        await _next(context);
    }

    private async Task<string?> ResolveTenantIdAsync(HttpContext context)
    {
        // C-01 SECURITY FIX: X-Tenant-Id header resolution REMOVED.
        // Tenant context is derived ONLY from subdomain, custom domain, or JWT claims.

        var host = context.Request.Host.Host;

        // 1. Subdomain
        var subdomain = GetSubdomain(host);
        if (!string.IsNullOrEmpty(subdomain) && subdomain != "app" && subdomain != "api" && subdomain != "www")
            return subdomain;

        // 2. Custom domain — WL-04: async + IMemoryCache, avoids per-request synchronous DB hit
        if (!string.IsNullOrEmpty(host) &&
            host != "upkilo.com" &&
            !host.EndsWith(".upkilo.com", StringComparison.OrdinalIgnoreCase) &&
            host != "localhost")
        {
            var cacheKey = $"{CustomDomainCachePrefix}{host}";

            if (!_cache.TryGetValue(cacheKey, out string? cachedTenantId))
            {
                var dbContext = context.RequestServices.GetRequiredService<Upkilo.Infrastructure.Data.AppDbContext>();
                cachedTenantId = await dbContext.CustomDomains
                    .IgnoreQueryFilters()
                    .Where(d => d.Hostname == host && d.IsVerified)
                    .Select(d => d.TenantId.ToString())
                    .FirstOrDefaultAsync();

                _cache.Set(cacheKey, cachedTenantId ?? string.Empty,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = CustomDomainCacheTtl
                    });
            }

            if (!string.IsNullOrEmpty(cachedTenantId))
                return cachedTenantId;
        }

        // 3. JWT claim
        var tenantClaim = context.User?.FindFirst("tenant_id");
        if (tenantClaim != null)
            return tenantClaim.Value;

        return null;
    }

    private static string? GetSubdomain(string host)
    {
        var parts = host.Split('.');
        return parts.Length >= 3 ? parts[0] : null;
    }
}
