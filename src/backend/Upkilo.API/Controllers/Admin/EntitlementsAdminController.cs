using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers.Admin;

/// <summary>
/// Platform-admin surface for customer-specific entitlements.
///
/// This is the supported way to give one customer something their plan does not include, or to
/// take something away from one customer without repricing the plan. It exists so that
/// commercial exceptions are DATA — auditable, expiring, reversible by support — instead of
/// per-customer branches in application code.
///
/// Every route is deliberately cross-tenant and therefore SuperAdmin-only. Reads use
/// IgnoreQueryFilters with an explicit TenantId because the ambient tenant filter would
/// otherwise scope every query to the admin's own tenant and silently return nothing.
/// </summary>
[ApiController]
[Route("api/admin/entitlements")]
[Authorize(Roles = "SuperAdmin")]
public class EntitlementsAdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEntitlementService _entitlements;
    private readonly ILogger<EntitlementsAdminController> _logger;

    public EntitlementsAdminController(
        AppDbContext context,
        IEntitlementService entitlements,
        ILogger<EntitlementsAdminController> logger)
    {
        _context = context;
        _entitlements = entitlements;
        _logger = logger;
    }

    /// <summary>
    /// The feature catalogue the admin UI renders its pickers from. Served from FeatureKeys
    /// joined to the database rows so a key present in one but not the other is visible here
    /// rather than only showing up as a mysteriously dead gate.
    /// </summary>
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var dbFeatures = await _context.PricingFeatures
            .AsNoTracking()
            .ToDictionaryAsync(f => f.Key, f => f, StringComparer.Ordinal, ct);

        var catalog = FeatureKeys.All.OrderBy(k => k, StringComparer.Ordinal).Select(key =>
        {
            dbFeatures.TryGetValue(key, out var f);
            return new
            {
                key,
                name = f?.Name ?? key,
                description = f?.Description ?? string.Empty,
                isNumeric = FeatureKeys.Numeric.Contains(key),
                // A key the code gates on but the database has never heard of. Every gate using
                // it denies unconditionally, so surfacing it is the difference between a
                // five-minute fix and a support ticket about a customer "losing" a feature.
                missingFromDatabase = f is null,
            };
        });

        var orphanedInDatabase = dbFeatures.Keys
            .Where(k => !FeatureKeys.IsKnown(k))
            .OrderBy(k => k, StringComparer.Ordinal);

        return Ok(new { features = catalog, orphanedInDatabase });
    }

    /// <summary>
    /// The effective entitlement inspector: for each feature, what the plan says, what any
    /// override says, and which of them won. This is the view that answers "why can this
    /// customer do that?" without anyone reading the database by hand.
    /// </summary>
    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> GetEffectiveEntitlements(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null) return NotFound(new { error = "Tenant not found" });

        var set = await _entitlements.GetEffectiveEntitlementsAsync(tenantId, ct);

        return Ok(new
        {
            tenantId,
            tenantName = tenant.Name,
            planName = set.PlanName,
            pricingPlanId = set.PricingPlanId,
            subscriptionStatus = set.SubscriptionStatus,
            isServiceEntitled = set.IsServiceEntitled,
            currentPeriodEnd = set.CurrentPeriodEnd,
            resolvedAt = set.ResolvedAt,
            features = set.Features.Values
                .OrderBy(e => e.Key, StringComparer.Ordinal)
                .Select(e => new
                {
                    key = e.Key,
                    effective = e.IsEnabled,
                    limit = e.Limit,
                    planValue = e.PlanValue,
                    source = e.Source.ToString(),
                    reason = DescribeSource(e),
                    overrideReason = e.Reason,
                    expiresAt = e.ExpiresAt,
                })
        });
    }

    /// <summary>Raw override rows for a tenant, including ones not yet in effect or expired.</summary>
    [HttpGet("{tenantId:guid}/overrides")]
    public async Task<IActionResult> GetOverrides(Guid tenantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var rows = await _context.Set<TenantFeatureOverride>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .OrderBy(o => o.FeatureKey)
            .ToListAsync(ct);

        return Ok(rows.Select(o => new
        {
            o.Id,
            o.FeatureKey,
            o.IsEnabled,
            o.NumericLimit,
            o.StartsAt,
            o.ExpiresAt,
            o.Reason,
            o.GrantedByUserId,
            o.CreatedAt,
            o.UpdatedAt,
            isActive = o.IsActiveAt(now),
            // Distinguishes "not started yet" from "already lapsed" — both are inactive, but
            // only one of them is a mistake worth chasing.
            status = !o.IsActiveAt(now)
                ? (o.StartsAt > now ? "scheduled" : "expired")
                : "active",
        }));
    }

    /// <summary>
    /// Cross-tenant audit of the one case the override design deliberately permits and cannot
    /// self-correct: a GRANT with no expiry.
    ///
    /// Overrides outrank the subscription-status gate on purpose — that is what makes a goodwill
    /// grant during a billing dispute possible. The cost is that an unbounded grant on a tenant
    /// who has since cancelled keeps serving them a paid feature indefinitely, and nothing in
    /// the resolver will ever notice, because from its point of view the override is doing
    /// exactly what it was told.
    ///
    /// So the leak is made discoverable rather than prevented. Rows where the subscription is no
    /// longer entitled to service are flagged: those are tenants receiving a paid feature with
    /// no billing behind it.
    /// </summary>
    [HttpGet("audit/unbounded-grants")]
    public async Task<IActionResult> AuditUnboundedGrants(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var grants = await _context.Set<TenantFeatureOverride>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.IsEnabled && o.ExpiresAt == null)
            .ToListAsync(ct);

        if (grants.Count == 0)
            return Ok(new { total = 0, unbilled = 0, grants = Array.Empty<object>() });

        var tenantIds = grants.Select(g => g.TenantId).Distinct().ToList();

        var tenants = await _context.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var statuses = await _context.Set<Subscription>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(sub => tenantIds.Contains(sub.TenantId))
            .Select(sub => new { sub.TenantId, sub.Status })
            .ToDictionaryAsync(x => x.TenantId, x => x.Status, ct);

        var rows = grants.Select(g =>
        {
            var hasStatus = statuses.TryGetValue(g.TenantId, out var status);
            // Mirrors EntitlementService.IsServiceEntitled. Kept as an explicit list rather than
            // calling the resolver per row, which would be one query per grant.
            var entitled = hasStatus && status is
                SubscriptionStatus.Active or
                SubscriptionStatus.Trialing or
                SubscriptionStatus.Trial or
                SubscriptionStatus.PastDue;

            return new
            {
                g.TenantId,
                tenantName = tenants.TryGetValue(g.TenantId, out var n) ? n : "(unknown)",
                g.FeatureKey,
                g.NumericLimit,
                g.Reason,
                g.GrantedByUserId,
                g.CreatedAt,
                subscriptionStatus = hasStatus ? status.ToString() : "None",
                // The actionable signal: a paid feature being served with no billing behind it.
                unbilled = !entitled,
                ageDays = (int)(now - g.CreatedAt).TotalDays,
            };
        })
        .OrderByDescending(r => r.unbilled)
        .ThenByDescending(r => r.ageDays)
        .ToList();

        return Ok(new
        {
            total = rows.Count,
            unbilled = rows.Count(r => r.unbilled),
            grants = rows,
        });
    }

    /// <summary>
    /// Creates or replaces the override for one (tenant, feature). Upsert rather than
    /// create-only: the unique index permits a single live row per pair, and support changing
    /// their mind about an existing grant is the normal case, not an error.
    /// </summary>
    [HttpPut("{tenantId:guid}/overrides/{featureKey}")]
    public async Task<IActionResult> UpsertOverride(
        Guid tenantId,
        string featureKey,
        [FromBody] UpsertOverrideRequest request,
        CancellationToken ct)
    {
        if (!FeatureKeys.IsKnown(featureKey))
        {
            // Rejected rather than stored. An override on an unknown key can never resolve, so
            // accepting it would silently promise the customer something that never arrives.
            return BadRequest(new
            {
                error = "Unknown feature key",
                message = $"'{featureKey}' is not in the entitlement catalogue.",
                validKeys = FeatureKeys.All.OrderBy(k => k, StringComparer.Ordinal),
            });
        }

        if (request.ExpiresAt is { } exp && exp <= DateTime.UtcNow)
            return BadRequest(new { error = "ExpiresAt must be in the future" });

        if (request.StartsAt is { } start && request.ExpiresAt is { } end && end <= start)
            return BadRequest(new { error = "ExpiresAt must be after StartsAt" });

        if (request.NumericLimit is { } lim && lim < EntitlementLimits.Unlimited)
            return BadRequest(new { error = "NumericLimit must be -1 (unlimited) or greater" });

        var tenantExists = await _context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tenantId, ct);
        if (!tenantExists) return NotFound(new { error = "Tenant not found" });

        var existing = await _context.Set<TenantFeatureOverride>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.FeatureKey == featureKey && !o.IsDeleted, ct);

        var before = existing is null ? null : Snapshot(existing);
        var adminUserId = CurrentUserId();

        if (existing is null)
        {
            existing = new TenantFeatureOverride
            {
                TenantId = tenantId,
                FeatureKey = featureKey,
            };
            _context.Add(existing);
        }

        existing.IsEnabled = request.IsEnabled;
        existing.NumericLimit = request.NumericLimit;
        existing.StartsAt = request.StartsAt;
        existing.ExpiresAt = request.ExpiresAt;
        existing.Reason = request.Reason;
        existing.GrantedByUserId = adminUserId;

        await _context.SaveChangesAsync(ct);
        await WriteAuditAsync(tenantId, featureKey, before, Snapshot(existing), adminUserId, ct);

        // The customer must see this on their very next request, not up to five minutes later.
        await _entitlements.InvalidateAsync(tenantId);

        _logger.LogInformation(
            "Entitlement override set: tenant {TenantId} feature {FeatureKey} enabled={Enabled} " +
            "limit={Limit} expires={Expires} by admin {AdminId}",
            tenantId, featureKey, request.IsEnabled, request.NumericLimit, request.ExpiresAt, adminUserId);

        return Ok(new { message = "Override saved", overrideId = existing.Id });
    }

    /// <summary>
    /// Removes the override so the feature reverts to whatever the plan says. Soft-deleted by
    /// the context's delete interception, which is why the unique index is filtered on
    /// IsDeleted — otherwise re-granting the same feature later would collide with this row.
    /// </summary>
    [HttpDelete("{tenantId:guid}/overrides/{featureKey}")]
    public async Task<IActionResult> DeleteOverride(Guid tenantId, string featureKey, CancellationToken ct)
    {
        var existing = await _context.Set<TenantFeatureOverride>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.FeatureKey == featureKey && !o.IsDeleted, ct);

        if (existing is null) return NotFound(new { error = "Override not found" });

        var before = Snapshot(existing);
        var adminUserId = CurrentUserId();

        _context.Remove(existing);
        await _context.SaveChangesAsync(ct);
        await WriteAuditAsync(tenantId, featureKey, before, null, adminUserId, ct);

        await _entitlements.InvalidateAsync(tenantId);

        _logger.LogInformation(
            "Entitlement override removed: tenant {TenantId} feature {FeatureKey} by admin {AdminId}",
            tenantId, featureKey, adminUserId);

        return Ok(new { message = "Override removed; feature reverts to plan default" });
    }

    /// <summary>
    /// Drops every tenant's cached entitlements. For use after editing a plan's feature
    /// mappings, which changes the answer for every tenant on that plan at once and has no
    /// per-tenant invalidation to hook.
    /// </summary>
    [HttpPost("cache/invalidate-all")]
    public async Task<IActionResult> InvalidateAll()
    {
        await _entitlements.InvalidateAllAsync();
        _logger.LogWarning("Entitlement cache invalidated for ALL tenants by admin {AdminId}", CurrentUserId());
        return Ok(new { message = "Entitlement cache invalidated for all tenants" });
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    private static string DescribeSource(Entitlement e) => e.Source switch
    {
        EntitlementSource.Override => e.IsEnabled
            ? "Granted by a customer-specific override"
            : "Revoked by a customer-specific override",
        EntitlementSource.Plan => "Included in the subscription plan",
        EntitlementSource.PlanExcluded => "Not included in the subscription plan",
        EntitlementSource.SubscriptionInactive => "Subscription is not entitled to service",
        EntitlementSource.NoSubscription => "Tenant has no subscription",
        _ => "Unknown",
    };

    private static string Snapshot(TenantFeatureOverride o) => JsonSerializer.Serialize(new
    {
        o.FeatureKey,
        o.IsEnabled,
        o.NumericLimit,
        o.StartsAt,
        o.ExpiresAt,
        o.Reason,
    });

    /// <summary>
    /// Records the change against the TARGET tenant, not the admin's own, so a customer's
    /// entitlement history reads as one timeline regardless of which admin touched it.
    /// </summary>
    private async Task WriteAuditAsync(
        Guid tenantId, string featureKey, string? before, string? after, Guid? adminUserId, CancellationToken ct)
    {
        _context.AuditLogsV2.Add(new AuditEntryV2
        {
            TenantId = tenantId,
            EntityType = nameof(TenantFeatureOverride),
            EntityId = featureKey,
            Action = after is null ? "EntitlementOverrideRemoved" : "EntitlementOverrideSet",
            UserId = adminUserId,
            UserName = User.Identity?.Name,
            OldValues = before,
            NewValues = after,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Details = $"Feature '{featureKey}' for tenant {tenantId}",
            Timestamp = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(ct);
    }
}

public class UpsertOverrideRequest
{
    /// <summary>True grants the feature, false revokes it. Both beat the plan.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Null inherits the plan's limit; -1 unlimited. Ignored for boolean features.</summary>
    public int? NumericLimit { get; set; }

    public DateTime? StartsAt { get; set; }

    /// <summary>Strongly encouraged for grants — an unbounded grant survives cancellation.</summary>
    public DateTime? ExpiresAt { get; set; }

    public string? Reason { get; set; }
}
