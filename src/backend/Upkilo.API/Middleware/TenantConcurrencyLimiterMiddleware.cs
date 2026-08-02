using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;

namespace Upkilo.API.Middleware;

/// <summary>
/// Tenant-based concurrency limiter middleware.
/// Enforces different concurrent request limits based on subscription tier:
///   Free:         10 concurrent requests
///   Starter:      30 concurrent requests
///   Professional: 100 concurrent requests
///   Enterprise:   1000 concurrent requests
///
/// Scope: the limit is enforced PER PROCESS. With multiple replicas the effective ceiling is
/// limit x replica count, so treat these as a per-instance safety valve against one tenant
/// saturating a single node — not as a global quota.
/// </summary>
public class TenantConcurrencyLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantConcurrencyLimiterMiddleware> _logger;

    /// <summary>
    /// Semaphore plus the tier it was sized for.
    ///
    /// The tier is stored because the entry has to be rebuilt when a tenant changes plan. The
    /// previous version called GetOrAdd with a factory that captured the limit, and the factory
    /// only runs when the key is absent — so a tenant who upgraded kept the semaphore built for
    /// their old tier and stayed throttled at the old limit until the process restarted.
    /// </summary>
    private sealed record TenantGate(string Tier, SemaphoreSlim Semaphore);

    private static readonly ConcurrentDictionary<string, TenantGate> _tenantSemaphores = new();

    // Keep in step with PricingSeeder and TierRateLimitMiddleware. An unmapped plan silently
    // falls back to the lowest limit, which is how a paying tier can end up throttled to free
    // capacity without anything being logged.
    private static readonly Dictionary<string, int> TierConcurrencyLimits = new()
    {
        { "Free", 10 },
        { "Starter", 30 },
        { "Growth", 150 },
        { "Enterprise", 1000 }
    };

    public TenantConcurrencyLimiterMiddleware(RequestDelegate next, ILogger<TenantConcurrencyLimiterMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Items["TenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId))
        {
            await _next(context);
            return;
        }

        var tierObj = context.Items["TenantTier"];
        var tier = tierObj?.ToString() ?? "Free";

        // Legacy plan names from before the tier consolidation, kept as a safety net for any
        // subscription row still carrying one.
        tier = tier switch
        {
            "Professional" or "Business" or "Agency" => "Growth",
            _ => tier
        };

        if (!TierConcurrencyLimits.TryGetValue(tier, out var limit))
        {
            limit = 10;
            _logger.LogWarning(
                "Unmapped plan '{Tier}' in TenantConcurrencyLimiterMiddleware — falling back to {Limit} concurrent. Add it to TierConcurrencyLimits.",
                tier, limit);
        }

        // AddOrUpdate rather than GetOrAdd: when the stored gate was sized for a different tier the
        // entry is replaced, so a plan change takes effect on the next request instead of at the
        // next process restart.
        //
        // The old gate is intentionally not disposed here. Requests admitted through it are still
        // in flight and will call Release() on it; disposing would throw underneath them. It is
        // unreferenced once those drain and is collected normally — SemaphoreSlim only holds an
        // unmanaged handle if AvailableWaitHandle was read, which this code never does.
        var gate = _tenantSemaphores.AddOrUpdate(
            tenantId,
            _ => new TenantGate(tier, new SemaphoreSlim(limit, limit)),
            (_, existing) => existing.Tier == tier
                ? existing
                : new TenantGate(tier, new SemaphoreSlim(limit, limit)));

        var semaphore = gate.Semaphore;

        if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            _logger.LogWarning("Concurrency limit ({Limit}) exceeded for tenant {TenantId} (tier: {Tier})", limit, tenantId, tier);

            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.Headers["Retry-After"] = "5";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many concurrent requests",
                tier,
                concurrencyLimit = limit,
                retryAfter = 5
            });
            return;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
