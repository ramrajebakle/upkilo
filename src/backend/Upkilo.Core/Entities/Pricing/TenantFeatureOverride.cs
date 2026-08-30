using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// A per-tenant deviation from the plan's entitlements — the data form of a customer-specific
/// deal.
///
/// This is the table that makes "give THIS customer AI insights without moving them to Growth"
/// expressible without a code change. It replaces the alternative that would otherwise appear
/// under commercial pressure — <c>if (tenantId == "…")</c> scattered through controllers —
/// which cannot be audited, cannot expire, and cannot be undone by anyone but an engineer.
///
/// It grants AND revokes: <see cref="IsEnabled"/> false on a feature the plan enables is how a
/// capability gets pulled from a single abusive tenant without repricing the plan for everyone
/// else on it.
///
/// PRECEDENCE. An override beats both the plan mapping and the subscription's lifecycle state.
/// That ordering is deliberate: the case an override exists to serve is precisely the one the
/// plan and the billing status get wrong — a grandfathered customer, a negotiated trial
/// extension, a goodwill grant during a payment dispute. Because it outranks the status gate,
/// an unbounded grant on a cancelled tenant keeps serving them for free, so
/// <see cref="ExpiresAt"/> is the primary control and the admin API nudges toward setting it.
/// EntitlementService reports <c>Source = Override</c> for anything resolved this way, and the
/// admin inspector shows the reason, so a grant is always traceable to a person and a date.
/// </summary>
public class TenantFeatureOverride : TenantEntity
{
    /// <summary>
    /// A key from <see cref="FeatureKeys.All"/>. Validated on write — an override on a key
    /// that no longer exists would be invisible in the UI and permanently unresolvable.
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    /// The effective on/off state while this override is in its active window. False is a
    /// genuine revoke, not "no opinion" — a row that meant "no opinion" would simply not exist.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Effective limit for a numeric feature. Null means "inherit the plan's limit", which
    /// lets an override flip a numeric feature on without having to restate its quantity.
    /// <see cref="EntitlementLimits.Unlimited"/> (-1) means unlimited.
    /// </summary>
    public int? NumericLimit { get; set; }

    /// <summary>
    /// When the override starts applying. Null = immediately. Lets a grant be staged ahead of
    /// a contract start date rather than remembered and applied by hand on the day.
    /// </summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>
    /// When the override stops applying. Null = permanent, which is the dangerous case for a
    /// grant and the reason the resolver reports it distinctly. Once passed, resolution falls
    /// straight back to the plan — nothing sweeps the table, so an expired row can never leave
    /// access switched on by omission.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Why this exists — shown in the admin inspector and the audit trail.</summary>
    public string? Reason { get; set; }

    /// <summary>The platform user who last wrote this row.</summary>
    public Guid? GrantedByUserId { get; set; }

    /// <summary>
    /// True while <paramref name="asOf"/> falls inside [StartsAt, ExpiresAt). Evaluated at read time
    /// rather than persisted, so an expiry needs no background job to take effect and cannot be
    /// left stale by one that failed to run.
    /// </summary>
    public bool IsActiveAt(DateTime asOf) =>
        (StartsAt is null || StartsAt <= asOf) &&
        (ExpiresAt is null || ExpiresAt > asOf);
}

/// <summary>
/// Sentinel values shared by plan mappings and overrides.
/// </summary>
public static class EntitlementLimits
{
    /// <summary>
    /// No ceiling. Matches the convention already used by UsageSummary and CheckUsageLimitAsync,
    /// where -1 short-circuits the comparison.
    /// </summary>
    public const int Unlimited = -1;

    /// <summary>Entitled to none of this resource.</summary>
    public const int None = 0;
}
