using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// The single authority on what a tenant may do.
///
/// Every gate — the [RequiresFeature] middleware, ISubscriptionService.CheckFeatureAccessAsync,
/// the usage/limit endpoints, the admin inspector and the payload the frontend renders from —
/// resolves through this one call, so the answer cannot differ between layers. Before this
/// existed the frontend read plan mappings from one shape and the backend from another, which
/// is how the UI and the API ended up disagreeing about the same feature for the same tenant.
///
/// Resolution is: subscription lifecycle state -> plan mappings -> tenant overrides.
/// <see cref="Entitlement.Source"/> reports which of those decided each key, which is what
/// makes support questions ("why can this customer do that?") answerable without a DBA.
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Resolves the complete effective entitlement set for a tenant. Cached; call
    /// <see cref="InvalidateAsync"/> after anything that could change the answer.
    /// </summary>
    Task<EntitlementSet> GetEffectiveEntitlementsAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// True when the tenant may use <paramref name="featureKey"/> right now. Unknown keys
    /// return false: a gate naming a feature that does not exist is a bug, and denying is the
    /// safe direction for it.
    /// </summary>
    Task<bool> HasFeatureAsync(Guid tenantId, string featureKey, CancellationToken ct = default);

    /// <summary>
    /// Effective ceiling for a numeric feature: -1 unlimited, 0 none. Returns 0 for a key the
    /// tenant is not entitled to and for keys outside <see cref="FeatureKeys.Numeric"/>.
    /// </summary>
    Task<int> GetLimitAsync(Guid tenantId, string featureKey, CancellationToken ct = default);

    /// <summary>Drops this tenant's cached resolution. Safe to call when nothing changed.</summary>
    Task InvalidateAsync(Guid tenantId);

    /// <summary>
    /// Invalidates EVERY tenant at once, for changes that are not tenant-scoped — an admin
    /// editing a plan's feature mappings changes the answer for every tenant on that plan, and
    /// there is no key pattern to scan for that in a distributed cache.
    /// </summary>
    Task InvalidateAllAsync();
}

/// <summary>What decided a particular key's effective value.</summary>
public enum EntitlementSource
{
    /// <summary>The tenant's plan enables it and the subscription is in good standing.</summary>
    Plan,

    /// <summary>The tenant's plan does not include it.</summary>
    PlanExcluded,

    /// <summary>
    /// The plan includes it but the subscription is not entitled to service — cancelled,
    /// expired, suspended or paused.
    /// </summary>
    SubscriptionInactive,

    /// <summary>A tenant-specific override decided it, beating both of the above.</summary>
    Override,

    /// <summary>No subscription row exists for the tenant at all.</summary>
    NoSubscription,
}

/// <summary>
/// One resolved feature, carrying enough provenance for the admin inspector to explain itself.
/// </summary>
public class Entitlement
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }

    /// <summary>-1 unlimited, 0 none. Only meaningful for <see cref="FeatureKeys.Numeric"/> keys.</summary>
    public int Limit { get; set; }

    public EntitlementSource Source { get; set; }

    /// <summary>Set only when <see cref="Source"/> is Override and the override expires.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Admin-supplied justification, when an override decided this.</summary>
    public string? Reason { get; set; }

    /// <summary>Whether the plan alone would have enabled it — the "before" in the audit view.</summary>
    public bool PlanValue { get; set; }
}

/// <summary>
/// A tenant's complete effective entitlements plus the billing context that produced them.
/// </summary>
public class EntitlementSet
{
    public Guid TenantId { get; set; }
    public Guid? PricingPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;

    /// <summary>Subscription status name, or "None" when no subscription row exists.</summary>
    public string SubscriptionStatus { get; set; } = "None";

    /// <summary>
    /// False when the lifecycle state means the tenant is not entitled to service. Overrides
    /// can still grant individual features regardless — see TenantFeatureOverride.
    /// </summary>
    public bool IsServiceEntitled { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>Every key in <see cref="FeatureKeys.All"/>, resolved. Never partial.</summary>
    public Dictionary<string, Entitlement> Features { get; set; } = new(StringComparer.Ordinal);

    /// <summary>When this snapshot was resolved. Surfaced so staleness is visible, not implied.</summary>
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The earliest future moment at which this resolution could change on its own — the nearest
    /// override expiry or scheduled start. Null when nothing is pending.
    ///
    /// This exists because time-based transitions are invisible to a cached snapshot. An expiring
    /// grant at least leaves its ExpiresAt in the snapshot, but a SCHEDULED override is simply
    /// absent from it — the resolver filtered it out as not-yet-active — so nothing in the cached
    /// value hints that it is due. Without this field a grant scheduled for 09:00 would not take
    /// effect until the entry aged out, up to five minutes late, and an admin watching for it
    /// would reasonably conclude the schedule had not worked.
    ///
    /// Recording the next transition lets a cache hit notice it is past due and re-resolve.
    /// </summary>
    public DateTime? NextTransitionAt { get; set; }

    public bool Has(string key) =>
        Features.TryGetValue(key, out var e) && e.IsEnabled;

    public int LimitOf(string key) =>
        Features.TryGetValue(key, out var e) && e.IsEnabled ? e.Limit : EntitlementLimits.None;

    /// <summary>Flat key -> bool projection, the shape the frontend feature gates consume.</summary>
    public Dictionary<string, bool> ToFlags() =>
        Features.ToDictionary(kv => kv.Key, kv => kv.Value.IsEnabled, StringComparer.Ordinal);
}
