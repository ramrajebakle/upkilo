using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Handles subscription plan downgrades by revoking features
/// that are no longer available in the lower tier.
/// Called by StripeWebhookController when a plan change is detected.
/// </summary>
public class SubscriptionDowngradeHandler
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionDowngradeHandler> _logger;

    public SubscriptionDowngradeHandler(AppDbContext context, ILogger<SubscriptionDowngradeHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Enforces feature limits when a tenant downgrades their plan.
    /// Deactivates excess resources that exceed the new plan's limits.
    /// </summary>
    public async Task HandleDowngradeAsync(Guid tenantId, string oldPlanName, string newPlanName,
        int newMaxStaff, int newMaxLocations, int newMaxServices, bool newWebhooks, bool newApiAccess)
    {
        _logger.LogInformation(
            "Processing downgrade for tenant {TenantId}: {OldPlan} → {NewPlan}",
            tenantId, oldPlanName, newPlanName);

        var changes = new List<string>();

        // 1. Staff limit enforcement
        //
        // Ordered OLDEST FIRST so that Skip(limit) retains the longest-standing records and
        // deactivates the most recently added — the ones that took the tenant over the limit.
        //
        // This was OrderByDescending, which did the exact opposite: downgrading a salon from 25
        // seats to 10 kept its ten newest hires and deactivated the fifteen longest-serving
        // staff, the owner among them. The same inversion applied to locations and services, so
        // a downgrade retired the original branch and kept the newest. The existing test only
        // asserted how MANY records survived, never which, so nothing caught it.
        if (newMaxStaff > 0)
        {
            var activeStaff = await _context.StaffMembers
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            if (activeStaff.Count > newMaxStaff)
            {
                var excess = activeStaff.Skip(newMaxStaff).ToList();
                foreach (var staff in excess) staff.IsActive = false;
                changes.Add($"Deactivated {excess.Count} staff members (over limit of {newMaxStaff})");
            }
        }

        // 2. Location limit enforcement
        if (newMaxLocations > 0)
        {
            var locations = await _context.Locations
                .Where(l => l.TenantId == tenantId && l.IsActive)
                .OrderBy(l => l.CreatedAt)   // oldest retained — see staff block above
                .ToListAsync();

            if (locations.Count > newMaxLocations)
            {
                var excess = locations.Skip(newMaxLocations).ToList();
                foreach (var loc in excess) loc.IsActive = false;
                changes.Add($"Deactivated {excess.Count} locations (over limit of {newMaxLocations})");
            }
        }

        // 3. Service limit enforcement
        if (newMaxServices > 0)
        {
            var services = await _context.Services
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderBy(s => s.CreatedAt)   // oldest retained — see staff block above
                .ToListAsync();

            if (services.Count > newMaxServices)
            {
                var excess = services.Skip(newMaxServices).ToList();
                foreach (var svc in excess) svc.IsActive = false;
                changes.Add($"Deactivated {excess.Count} services (over limit of {newMaxServices})");
            }
        }

        // 4. Webhook access revocation
        if (!newWebhooks)
        {
            var webhooks = await _context.Webhooks
                .Where(w => w.TenantId == tenantId && w.IsActive)
                .ToListAsync();
            foreach (var wh in webhooks) wh.IsActive = false;
            if (webhooks.Count > 0)
                changes.Add($"Disabled {webhooks.Count} webhooks (not available in {newPlanName})");
        }

        // 5. API key access revocation
        if (!newApiAccess)
        {
            var keys = await _context.ApiKeys
                .Where(k => k.TenantId == tenantId && k.IsActive)
                .ToListAsync();
            foreach (var key in keys) key.IsActive = false;
            if (keys.Count > 0)
                changes.Add($"Disabled {keys.Count} API keys (not available in {newPlanName})");
        }

        if (changes.Count > 0)
        {
            await _context.SaveChangesAsync();
            _context.AuditEntries.Add(new AuditEntry
            {
                TenantId = tenantId,
                Action = "SubscriptionDowngrade",
                EntityType = "Subscription",
                Details = $"Plan changed from {oldPlanName} to {newPlanName}. Actions: {string.Join("; ", changes)}"
            });
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Downgrade processing complete for tenant {TenantId}: {Count} changes applied",
            tenantId, changes.Count);
    }
}
