using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

/// <summary>
/// Controller for handling in-app upgrade logic, contextual upsell triggers, and plan comparison data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UpgradeController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public UpgradeController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Gets a set of contextual upsell flags indicating if the user has hit feature limits on their current tier.
    /// (Task 44 & 50)
    /// </summary>
    [HttpGet("contextual-flags")]
    public async Task<IActionResult> GetContextualUpsellFlags()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound();

        var subscription = await _context.Subscriptions
            .Include(s => s.PricingPlan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        var plan = subscription?.PricingPlan?.Name ?? tenant.SubscriptionTier.ToString();

        // 6 Contextual Upsell Triggers Logic
        var staffCount = await _context.StaffMembers.CountAsync(s => s.TenantId == tenantId);
        var locationCount = await _context.Locations.CountAsync(l => l.TenantId == tenantId);
        var hasSmsCampaigns = await _context.Campaigns.AnyAsync(c => c.TenantId == tenantId && c.Type == "SMS");
        var hasApiKeys = await _context.Set<Upkilo.Core.Entities.ApiKey>().AnyAsync(k => k.TenantId == tenantId);

        var triggers = new
        {
            RequiresStaffUpgrade = plan == "Free" && staffCount >= 2,
            RequiresLocationUpgrade = plan != "Enterprise" && locationCount >= 3,
            RequiresMarketingUpgrade = (plan == "Free" || plan == "Starter") && hasSmsCampaigns,
            // API access, advanced reports and white-label all unlock at Growth (see
            // PricingSeeder). Legacy plan names are included so tenants on pre-consolidation
            // rows are not nagged to upgrade to something they already have.
            RequiresApiAccess = plan is not ("Growth" or "Business" or "Agency" or "Enterprise") && hasApiKeys,
            RequiresAdvancedReports = plan is not ("Growth" or "Professional" or "Business" or "Agency" or "Enterprise"),
            RequiresWhiteLabel = plan is not ("Growth" or "Business" or "Agency" or "Enterprise")
        };

        return Ok(triggers);
    }

    /// <summary>
    /// Serves comparison data for upselling modals.
    /// (Task 45)
    /// </summary>
    [HttpGet("plan-comparison")]
    public async Task<IActionResult> GetPlanComparison()
    {
        var plans = await _context.PricingPlans
            .Include(p => p.Prices)
            .Include(p => p.FeatureMappings).ThenInclude(m => m.PricingFeature)
            .Where(p => p.IsActive)
            .ToListAsync();

        return Ok(plans);
    }
}
