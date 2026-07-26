using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Infrastructure.HealthChecks;

/// <summary>
/// Reports the state of the published pricing catalogue.
///
/// Registered because a broken price list produces no errors on its own: the API returns 200 with
/// null amounts and the pricing page renders "Contact us" everywhere. Without a probe, the first
/// signal that Upkilo has stopped selling is an absence of signups.
///
/// Tagged "ready" rather than "live" — the process is healthy, but it should not be considered
/// ready to take traffic while its pricing is unbuyable.
/// </summary>
public class PricingHealthCheck : IHealthCheck
{
    private readonly PricingIntegrityService _pricing;
    private readonly ILogger<PricingHealthCheck> _logger;

    // Result cache. /ready is a readiness probe — polled every few seconds, per replica, forever —
    // while the pricing catalogue only changes when an admin edits it. Without this the probe ran
    // a database query on every poll for data that is effectively static.
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(60);
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static IReadOnlyList<PricingIssue>? _cached;
    private static DateTime _cachedAtUtc = DateTime.MinValue;

    public PricingHealthCheck(PricingIntegrityService pricing, ILogger<PricingHealthCheck> logger)
    {
        _pricing = pricing;
        _logger = logger;
    }

    private async Task<IReadOnlyList<PricingIssue>> GetIssuesAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _cachedAtUtc < CacheFor)
            return _cached;

        await CacheLock.WaitAsync(ct);
        try
        {
            // Re-check inside the lock: concurrent probes would otherwise all miss and each run
            // the query.
            if (_cached is not null && DateTime.UtcNow - _cachedAtUtc < CacheFor)
                return _cached;

            _cached = await _pricing.ValidateAsync(ct);
            _cachedAtUtc = DateTime.UtcNow;
            return _cached;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = await GetIssuesAsync(cancellationToken);

            var critical = issues.Where(i => i.Severity == PricingIssueSeverity.Critical).ToList();
            var warnings = issues.Where(i => i.Severity == PricingIssueSeverity.Warning).ToList();

            var data = new Dictionary<string, object>
            {
                ["critical"] = critical.Count,
                ["warnings"] = warnings.Count
            };

            if (critical.Count > 0)
            {
                data["issues"] = critical.Select(i => $"{i.Code}: {i.Message}").ToArray();
                _logger.LogError("Pricing catalogue has {Count} critical issue(s): {Issues}",
                    critical.Count, string.Join(" | ", critical.Select(i => i.Code)));
                return HealthCheckResult.Unhealthy(
                    $"Pricing catalogue has {critical.Count} critical issue(s).", data: data);
            }

            if (warnings.Count > 0)
            {
                data["issues"] = warnings.Select(i => $"{i.Code}: {i.Message}").ToArray();
                return HealthCheckResult.Degraded(
                    $"Pricing catalogue has {warnings.Count} warning(s).", data: data);
            }

            return HealthCheckResult.Healthy("Pricing catalogue is valid.", data);
        }
        catch (Exception ex)
        {
            // A failure to *check* pricing is not the same as pricing being broken; report it
            // separately so it is not mistaken for an empty catalogue.
            _logger.LogError(ex, "Pricing integrity check could not run");
            return HealthCheckResult.Degraded("Could not evaluate pricing catalogue.", ex);
        }
    }
}
