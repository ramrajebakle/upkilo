
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Attributes;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/agency")]
[Authorize(Roles = "Owner,Admin")]
[FeatureGuard("white_label")]
public class AgencyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AgencyController> _logger;

    public AgencyController(AppDbContext context, ITenantProvider tenantProvider, ILogger<AgencyController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// List all sub-accounts managed by this agency tenant
    /// </summary>
    [HttpGet("subtenants")]
    public async Task<IActionResult> ListSubTenants()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.ParentTenantId == tenantId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.Status,
                t.SubscriptionTier,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = tenants, total = tenants.Count });
    }

    /// <summary>
    /// Create a new sub-account under this agency
    /// </summary>
    [HttpPost("subtenants")]
    public async Task<IActionResult> CreateSubTenant([FromBody] CreateSubTenantDto request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify slug uniqueness
        if (await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == request.Slug))
        {
            return BadRequest("Slug already taken.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ParentTenantId = tenantId,
            Name = request.Name,
            Slug = request.Slug,
            Email = request.Email,
            Status = TenantStatus.Active,
            SubscriptionTier = SubscriptionTier.Starter,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sub-tenant {SubTenantId} created by agency {AgencyId}", tenant.Id, tenantId);

        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status,
            tenant.CreatedAt
        });
    }

    /// <summary>
    /// Get details of a specific sub-account
    /// </summary>
    [HttpGet("subtenants/{id}")]
    public async Task<IActionResult> GetSubTenant(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.ParentTenantId == tenantId);

        if (subTenant == null) return NotFound();

        return Ok(new
        {
            subTenant.Id,
            subTenant.Name,
            subTenant.Slug,
            subTenant.Email,
            subTenant.Status,
            subTenant.SubscriptionTier,
            subTenant.CreatedAt
        });
    }

    /// <summary>
    /// Suspend or activate a sub-account
    /// </summary>
    [HttpPut("subtenants/{id}/status")]
    public async Task<IActionResult> UpdateSubTenantStatus(Guid id, [FromBody] UpdateStatusDto request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.ParentTenantId == tenantId);

        if (subTenant == null) return NotFound();

        subTenant.Status = request.Status;
        subTenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sub-tenant {SubTenantId} status changed to {Status}", id, request.Status);
        return Ok(new { success = true, status = subTenant.Status });
    }

    /// <summary>
    /// AG1: Agency dashboard — aggregated metrics across all sub-tenants.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetAgencyDashboard()
    {
        var agencyId = _tenantProvider.GetTenantId();
        if (agencyId == null) return Unauthorized();

        var subTenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.ParentTenantId == agencyId)
            .Select(t => new { t.Id, t.Name, t.Slug, t.Status, t.SubscriptionTier, t.AverageRating, t.ReviewCount, t.CreatedAt })
            .ToListAsync();

        var subIds = subTenants.Select(t => t.Id).ToList();

        var totalBookings = await _context.Bookings.CountAsync(b => subIds.Contains(b.TenantId));
        var totalRevenue = await _context.Payments
            .Where(p => subIds.Contains(p.TenantId) && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var activeClients = await _context.Clients.CountAsync(c => subIds.Contains(c.TenantId));

        return Ok(new
        {
            totalSubTenants = subTenants.Count,
            activeSubTenants = subTenants.Count(t => t.Status == TenantStatus.Active),
            aggregatedRevenue = totalRevenue,
            aggregatedBookings = totalBookings,
            aggregatedClients = activeClients,
            subTenants = subTenants.Select(t => new
            {
                t.Id, t.Name, t.Slug, t.Status,
                tier = t.SubscriptionTier.ToString(),
                t.AverageRating, t.ReviewCount, t.CreatedAt
            })
        });
    }

    /// <summary>
    /// AG2: Client provisioning wizard — creates a sub-tenant + admin user + welcome email in one call.
    /// </summary>
    [HttpPost("provision-client")]
    public async Task<IActionResult> ProvisionClient([FromBody] ProvisionClientRequest request)
    {
        var agencyId = _tenantProvider.GetTenantId();
        if (agencyId == null) return Unauthorized();

        // Verify agency has capacity (max sub-tenants from plan)
        var currentSubCount = await _context.Tenants.IgnoreQueryFilters()
            .CountAsync(t => t.ParentTenantId == agencyId);

        if (currentSubCount >= 20)
            return BadRequest(new { error = "max_subtenants_reached", message = "Agency plan allows up to 20 sub-accounts. Contact support to increase." });

        var slug = request.Slug.ToLowerInvariant().Trim().Replace(" ", "-");
        if (await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug))
            return Conflict(new { error = "slug_taken", message = $"The slug '{slug}' is already taken." });

        // 1. Create sub-tenant
        var subTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ParentTenantId = agencyId,
            Name = request.BusinessName.Trim(),
            Slug = slug,
            Email = request.OwnerEmail.Trim().ToLower(),
            Phone = request.Phone,
            Industry = request.Industry,
            Status = TenantStatus.Active,
            SubscriptionTier = SubscriptionTier.Starter,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(subTenant);

        // 2. Create admin user with temp password (force change on first login via Preferences flag)
        var tempPassword = Guid.NewGuid().ToString("N")[..12] + "Aa1!";
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = subTenant.Id,
            Email = request.OwnerEmail.Trim().ToLower(),
            PasswordHash = hasher.HashPassword(new User(), tempPassword),
            FirstName = request.OwnerFirstName.Trim(),
            LastName = request.OwnerLastName.Trim(),
            Role = UserRole.Owner,
            Status = UserStatus.Active,
            EmailVerified = true,
            Preferences = new Dictionary<string, object> { ["mustChangePassword"] = true },
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        _logger.LogInformation("[AG2] Agency {AgencyId} provisioned client {SubTenantId} ({Email})",
            agencyId, subTenant.Id, request.OwnerEmail);

        return Ok(new
        {
            subTenantId = subTenant.Id,
            name = subTenant.Name,
            slug = subTenant.Slug,
            ownerEmail = user.Email,
            temporaryPassword = tempPassword,
            loginUrl = $"https://app.upkilo.com/login?email={Uri.EscapeDataString(user.Email)}",
            message = "Client account created. Share the temporary password securely — they must change it on first login."
        });
    }

    /// <summary>
    /// AG3: GET /agency/reseller-pricing — returns this agency's white-label pricing overrides.
    /// Allows agencies to set custom prices they charge their sub-tenants (markup over base cost).
    /// </summary>
    [HttpGet("reseller-pricing")]
    public async Task<IActionResult> GetResellerPricing()
    {
        var agencyId = _tenantProvider.GetTenantId();
        if (agencyId == null) return Unauthorized();

        var agency = await _context.Tenants.FindAsync(agencyId.Value);
        if (agency == null) return NotFound();

        var pricing = agency.Settings.TryGetValue("reseller_pricing", out var p)
            ? p
            : null;

        return Ok(new
        {
            agencyId,
            resellerPricing = pricing,
            basePlans = new[]
            {
                new { plan = "Starter", baseCost = 29, suggestedMarkup = "30-50%", suggestedPrice = "40-45" },
                new { plan = "Professional", baseCost = 79, suggestedMarkup = "20-40%", suggestedPrice = "95-110" },
                new { plan = "Business", baseCost = 149, suggestedMarkup = "15-30%", suggestedPrice = "170-195" }
            },
            hint = "Call PATCH /agency/reseller-pricing to save your custom pricing."
        });
    }

    /// <summary>
    /// AG3: PATCH /agency/reseller-pricing — saves custom reseller pricing tiers.
    /// </summary>
    [HttpPatch("reseller-pricing")]
    public async Task<IActionResult> SetResellerPricing([FromBody] ResellerPricingRequest request)
    {
        var agencyId = _tenantProvider.GetTenantId();
        if (agencyId == null) return Unauthorized();

        var agency = await _context.Tenants.FindAsync(agencyId.Value);
        if (agency == null) return NotFound();

        agency.Settings["reseller_pricing"] = System.Text.Json.JsonSerializer.Deserialize<object>(
            System.Text.Json.JsonSerializer.Serialize(request.Pricing));
        agency.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("[AG3] Agency {AgencyId} updated reseller pricing", agencyId);
        return Ok(new { saved = true, message = "Reseller pricing updated." });
    }

    /// <summary>
    /// Generate an impersonation link for a sub-account
    /// </summary>
    [HttpPost("subtenants/{id}/impersonate")]
    public async Task<IActionResult> ImpersonateSubTenant(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.ParentTenantId == tenantId);

        if (subTenant == null) return NotFound();

        // Return tenant ID to switch context — frontend handles token swap
        return Ok(new { switchToTenantId = subTenant.Id, tenantName = subTenant.Name });
    }
}

public class CreateSubTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateStatusDto
{
    public TenantStatus Status { get; set; }
}

public class ProvisionClientRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Industry { get; set; }
}

public class ResellerPricingRequest
{
    public Dictionary<string, object> Pricing { get; set; } = new();
}
