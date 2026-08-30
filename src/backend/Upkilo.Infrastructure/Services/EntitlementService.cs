using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// The one place a tenant's effective entitlements are computed. See IEntitlementService for
/// why every gate routes through here rather than reading plan mappings for itself.
/// </summary>
public class EntitlementService : IEntitlementService
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EntitlementService> _logger;

    /// <summary>
    /// Bounded staleness for the resolved set. Every mutation path we control also invalidates
    /// explicitly; this TTL is the backstop for the ones we do not — a row edited straight in
    /// the database, or an invalidation lost because Redis was briefly unreachable.
    ///
    /// Five minutes matches the window the previous sub:{tenantId} cache already ran with, so
    /// this is not a new exposure, and it is short enough that a missed invalidation is a
    /// support annoyance rather than a billing incident.
    /// </summary>
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Bumped when a change invalidates every tenant at once. It is part of the cache key, so
    /// incrementing it strands every previously cached entry without needing to enumerate keys
    /// — which IDistributedCache cannot do, and which would be an O(tenants) write storm even
    /// against raw Redis.
    ///
    /// Losing this counter (a Redis restart) resets it to 0 and could in principle re-expose
    /// entries written under an earlier epoch 0. Those entries carry the 5-minute TTL above, so
    /// the worst case is bounded by it rather than unbounded.
    /// </summary>
    private const string EpochKey = "ent:epoch";

    public EntitlementService(
        AppDbContext context,
        IDistributedCache cache,
        ILogger<EntitlementService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EntitlementSet> GetEffectiveEntitlementsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var key = await CacheKeyAsync(tenantId, ct);

        var cached = await SafeGetAsync(key, ct);
        if (cached != null)
        {
            try
            {
                var hit = JsonSerializer.Deserialize<EntitlementSet>(cached, JsonOpts);
                // A cached set can outlive a time-based transition inside the TTL window — an
                // override expiring, or a scheduled one falling due — so the snapshot is only
                // trusted while its next transition is still in the future.
                if (hit != null && (hit.NextTransitionAt is null || hit.NextTransitionAt > DateTime.UtcNow))
                    return hit;
            }
            catch (JsonException)
            {
                // Shape changed across a deploy. Fall through and recompute.
            }
        }

        var resolved = await ResolveAsync(tenantId, ct);

        try
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(resolved), CacheOptions, ct);
        }
        catch (Exception ex)
        {
            // Never fail a request because the cache is down — resolution already succeeded.
            _logger.LogWarning(ex, "Entitlement cache write failed for tenant {TenantId}", tenantId);
        }

        return resolved;
    }

    public async Task<bool> HasFeatureAsync(Guid tenantId, string featureKey, CancellationToken ct = default)
    {
        if (!FeatureKeys.IsKnown(featureKey))
        {
            // A gate naming a key outside the catalogue is a coding defect. Deny, and make it
            // loud: this is exactly the class of bug that silently refused every paying
            // customer before FeatureKeys existed, and it must never fail quietly again.
            _logger.LogError(
                "Entitlement check for unknown feature key '{FeatureKey}' (tenant {TenantId}). " +
                "Gate names must come from FeatureKeys. Denying.",
                featureKey, tenantId);
            return false;
        }

        var set = await GetEffectiveEntitlementsAsync(tenantId, ct);
        return set.Has(featureKey);
    }

    public async Task<int> GetLimitAsync(Guid tenantId, string featureKey, CancellationToken ct = default)
    {
        if (!FeatureKeys.IsKnown(featureKey)) return EntitlementLimits.None;
        var set = await GetEffectiveEntitlementsAsync(tenantId, ct);
        return set.LimitOf(featureKey);
    }

    public async Task InvalidateAsync(Guid tenantId)
    {
        try
        {
            await _cache.RemoveAsync(await CacheKeyAsync(tenantId, CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entitlement cache invalidation failed for tenant {TenantId}", tenantId);
        }
    }

    public async Task InvalidateAllAsync()
    {
        try
        {
            var current = await _cache.GetStringAsync(EpochKey);
            var next = (long.TryParse(current, out var e) ? e : 0) + 1;
            // No expiry: the epoch must outlive every entry it invalidates, otherwise expiring
            // it would roll the key space back onto stale entries.
            await _cache.SetStringAsync(EpochKey, next.ToString());

            // Adopt the new epoch immediately on this instance rather than waiting out the memo.
            // Without this the admin who just edited a plan would keep seeing the old
            // entitlements for a few seconds and reasonably conclude the change had not applied.
            _epochMemo = next.ToString();
            _epochMemoUntil = DateTime.UtcNow.Add(EpochMemoTtl);

            _logger.LogInformation("Entitlement cache invalidated globally (epoch {Epoch})", next);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Global entitlement cache invalidation failed");
        }
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    /// <summary>
    /// subscription lifecycle -> plan mappings -> tenant overrides, in that order.
    /// Always returns every key in the catalogue so callers never have to distinguish
    /// "absent" from "disabled" — a distinction that produced inconsistent gates previously.
    /// </summary>
    private async Task<EntitlementSet> ResolveAsync(Guid tenantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var subscription = await _context.Set<Subscription>()
            .AsNoTracking()
            .Include(s => s.PricingPlan)
                .ThenInclude(p => p!.FeatureMappings)
                    .ThenInclude(fm => fm.PricingFeature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        // Overrides are scoped by explicit TenantId rather than relying on the ambient query
        // filter: this resolver also runs from background jobs and webhook handlers, where
        // there is no ambient tenant and a filtered query would silently return nothing.
        var overrides = await _context.Set<TenantFeatureOverride>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .ToListAsync(ct);

        var serviceEntitled = IsServiceEntitled(subscription?.Status);

        var set = new EntitlementSet
        {
            TenantId = tenantId,
            PricingPlanId = subscription?.PricingPlanId,
            PlanName = subscription?.PricingPlan?.Name ?? string.Empty,
            SubscriptionStatus = subscription?.Status.ToString() ?? "None",
            IsServiceEntitled = serviceEntitled,
            CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
            ResolvedAt = now,
        };

        var planMappings = subscription?.PricingPlan?.FeatureMappings
            .Where(fm => fm.PricingFeature != null)
            .GroupBy(fm => fm.PricingFeature!.Key, StringComparer.Ordinal)
            // A plan with two rows for one feature is a data defect; take the enabling row so a
            // duplicate cannot silently revoke something the customer is paying for.
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.IsEnabled).First(), StringComparer.Ordinal)
            ?? new Dictionary<string, PlanFeatureMapping>(StringComparer.Ordinal);

        var overrideByKey = overrides
            .Where(o => o.IsActiveAt(now))
            .GroupBy(o => o.FeatureKey, StringComparer.Ordinal)
            // Newest wins if the unique index was ever bypassed (legacy rows, raw SQL).
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.UpdatedAt).First(), StringComparer.Ordinal);

        foreach (var key in FeatureKeys.All)
        {
            planMappings.TryGetValue(key, out var mapping);

            var planEnabled = mapping?.IsEnabled == true;
            var planLimit = ResolvePlanLimit(mapping);

            var entitlement = new Entitlement
            {
                Key = key,
                PlanValue = planEnabled,
            };

            if (overrideByKey.TryGetValue(key, out var ovr))
            {
                // Deliberately outranks the lifecycle gate — see TenantFeatureOverride.
                entitlement.IsEnabled = ovr.IsEnabled;
                entitlement.Limit = ovr.IsEnabled
                    ? (ovr.NumericLimit ?? planLimit)
                    : EntitlementLimits.None;
                entitlement.Source = EntitlementSource.Override;
                entitlement.ExpiresAt = ovr.ExpiresAt;
                entitlement.Reason = ovr.Reason;
            }
            else if (subscription == null)
            {
                entitlement.IsEnabled = false;
                entitlement.Limit = EntitlementLimits.None;
                entitlement.Source = EntitlementSource.NoSubscription;
            }
            else if (!serviceEntitled)
            {
                // The gap that made this whole class necessary: resolution used to consult the
                // plan and never the status, so a cancelled tenant kept every paid feature.
                entitlement.IsEnabled = false;
                entitlement.Limit = EntitlementLimits.None;
                entitlement.Source = EntitlementSource.SubscriptionInactive;
            }
            else
            {
                entitlement.IsEnabled = planEnabled;
                entitlement.Limit = planEnabled ? planLimit : EntitlementLimits.None;
                entitlement.Source = planEnabled
                    ? EntitlementSource.Plan
                    : EntitlementSource.PlanExcluded;
            }

            set.Features[key] = entitlement;
        }

        ApplyExpansionSeats(set, subscription);
        set.NextTransitionAt = NextTransition(overrides, now);

        return set;
    }

    /// <summary>
    /// Paid-for extra seats and locations are additive on top of whatever decided the base
    /// limit, matching how GetUsageAsync has always reported them. Skipped for unlimited, where
    /// adding would be meaningless, and skipped when the feature resolved to disabled, so
    /// expansion seats cannot resurrect a capability the tenant is not entitled to.
    /// </summary>
    private static void ApplyExpansionSeats(EntitlementSet set, Subscription? subscription)
    {
        if (subscription == null) return;

        AddSeats(FeatureKeys.MaxStaff, subscription.ExtraStaffCount);
        AddSeats(FeatureKeys.MaxLocations, subscription.ExtraLocationCount);

        void AddSeats(string key, int extra)
        {
            if (extra <= 0) return;
            if (!set.Features.TryGetValue(key, out var e)) return;
            if (!e.IsEnabled) return;
            if (e.Limit == EntitlementLimits.Unlimited) return;
            e.Limit += extra;
        }
    }

    /// <summary>
    /// NumericLimit null on an enabled mapping means unlimited — the convention PricingSeeder
    /// already uses for Growth's max_clients and Enterprise's staff and locations.
    /// </summary>
    private static int ResolvePlanLimit(PlanFeatureMapping? mapping)
    {
        if (mapping is null || !mapping.IsEnabled) return EntitlementLimits.None;
        return mapping.NumericLimit ?? EntitlementLimits.Unlimited;
    }

    /// <summary>
    /// Whether a lifecycle state entitles the tenant to service.
    ///
    /// PastDue is allowed on purpose and matches the rule already documented in
    /// SubscriptionService.GetUsageAsync: DunningAutomationJob runs a 14-day recovery timeline
    /// and flips the status to Suspended itself. Cutting features the moment a card declines
    /// would pre-empt that and churn customers over a retryable failure.
    ///
    /// Paused is NOT entitled: a paused subscription is not being billed, so continuing to
    /// serve paid features would be giving the product away for the length of the pause.
    /// </summary>
    private static bool IsServiceEntitled(SubscriptionStatus? status) => status switch
    {
        SubscriptionStatus.Active => true,
        SubscriptionStatus.Trialing => true,
        SubscriptionStatus.Trial => true,     // Stripe-mapping alias
        SubscriptionStatus.PastDue => true,   // within dunning grace
        _ => false,                           // Paused, Suspended, Cancelled, Expired
    };

    /// <summary>
    /// The nearest future moment at which these override rows would change the answer: a
    /// scheduled override starting, or an active one expiring.
    ///
    /// Computed from ALL rows, not just the currently-active ones, because a not-yet-active row
    /// is precisely the case the cached snapshot cannot otherwise represent — it never appears
    /// in the resolved features at all.
    /// </summary>
    private static DateTime? NextTransition(List<TenantFeatureOverride> overrides, DateTime now)
    {
        DateTime? next = null;

        void Consider(DateTime? candidate)
        {
            if (candidate is not { } c || c <= now) return;
            if (next is null || c < next) next = c;
        }

        foreach (var o in overrides)
        {
            Consider(o.StartsAt);    // pending activation
            Consider(o.ExpiresAt);   // pending expiry
        }

        return next;
    }

    /// <summary>
    /// Process-local memo of the epoch, refreshed at most once every <see cref="EpochMemoTtl"/>.
    ///
    /// The epoch is part of every cache key, so reading it from Redis on each call would put TWO
    /// round-trips on a path the enforcement middleware runs for every authenticated request —
    /// doubling the latency of what is meant to be a single cheap lookup. Memoising it keeps the
    /// common case at one round-trip.
    ///
    /// The cost is that a global invalidation takes up to the memo TTL to reach an instance
    /// whose memo is still warm. Seconds, against the five-minute entry TTL it supersedes, and
    /// bounded either way. Per-tenant invalidation is unaffected: it removes the key directly.
    ///
    /// Static because the epoch is global, not per-scope, and this service is scoped — a
    /// per-instance memo would be discarded after every request and buy nothing.
    /// </summary>
    private static readonly TimeSpan EpochMemoTtl = TimeSpan.FromSeconds(5);
    private static string _epochMemo = "0";
    private static DateTime _epochMemoUntil = DateTime.MinValue;

    private async Task<string> CacheKeyAsync(Guid tenantId, CancellationToken ct)
        => $"ent:{await GetEpochAsync(ct)}:{tenantId}";

    private async Task<string> GetEpochAsync(CancellationToken ct)
    {
        // Torn reads across threads are harmless: both values are valid epochs, and the worst
        // outcome is one extra resolve. Locking a hot path to avoid that would cost more than
        // the miss.
        if (DateTime.UtcNow < _epochMemoUntil) return _epochMemo;

        try
        {
            _epochMemo = await _cache.GetStringAsync(EpochKey, ct) ?? "0";
        }
        catch (Exception ex)
        {
            // Keep serving the last known epoch rather than silently falling back to "0", which
            // would resurrect the whole pre-invalidation key space while Redis is degraded.
            _logger.LogWarning(ex, "Entitlement epoch read failed; reusing last known epoch {Epoch}", _epochMemo);
        }

        _epochMemoUntil = DateTime.UtcNow.Add(EpochMemoTtl);
        return _epochMemo;
    }

    private async Task<string?> SafeGetAsync(string key, CancellationToken ct)
    {
        try
        {
            return await _cache.GetStringAsync(key, ct);
        }
        catch (Exception ex)
        {
            // Cache unavailable — resolve from the database rather than failing the request.
            _logger.LogWarning(ex, "Entitlement cache read failed for {Key}", key);
            return null;
        }
    }
}
