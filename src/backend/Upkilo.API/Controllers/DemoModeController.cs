using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Middleware;
using Microsoft.Extensions.Hosting;

namespace Upkilo.API.Controllers;

/// <summary>
/// Demo / Sandbox mode management.
/// Allows admins to toggle IsSandbox flag on a tenant and seed demo data.
/// The frontend reads GET /api/v1/demo/status to display the banner.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/demo")]
// Authenticated rather than Owner-only at the class level. The read-only `status` action
// backs DemoModeBanner, which the dashboard layout renders for EVERY user on EVERY page —
// so an Owner-only gate here produced a 403 per page load for every Admin, Manager and
// Staff session. The banner swallows the error, so the only visible symptom was console
// noise, but it also meant non-owners were never told they were looking at demo data,
// which is precisely who most needs telling.
//
// The three mutating actions below keep their own [Authorize(Roles = "Owner")]: enabling,
// disabling and seeding demo mode rewrite tenant data and stay with the owner.
[Authorize]
public class DemoModeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DemoModeController> _logger;
    private readonly IHostEnvironment _env;

    public DemoModeController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        ILogger<DemoModeController> logger,
        IHostEnvironment env)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _env = env;
    }

    // Helper: get or create the TenantManagement record
    private async Task<TenantManagement> GetOrCreateManagementAsync(Guid tenantId)
    {
        var mgmt = await _db.TenantManagements
            .FirstOrDefaultAsync(m => m.TenantId == tenantId);
        if (mgmt == null)
        {
            mgmt = new TenantManagement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Status = "Active",
                IsSandbox = false
            };
            _db.TenantManagements.Add(mgmt);
        }
        return mgmt;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/demo/status
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _db.Tenants
            .Where(t => t.Id == tenantId.Value)
            .Select(t => new { t.Id, t.Name, plan = t.Tier ?? t.SubscriptionTier.ToString() })
            .FirstOrDefaultAsync();

        if (tenant == null) return NotFound();

        var mgmt = await _db.TenantManagements
            .FirstOrDefaultAsync(m => m.TenantId == tenantId.Value);

        var isSandbox = mgmt?.IsSandbox ?? false;

        return Ok(ApiResponse<object>.Ok(new
        {
            isSandbox,
            tenantName = tenant.Name,
            plan = tenant.plan,
            message = isSandbox
                ? "You are in DEMO mode. Data changes are isolated and will not affect production."
                : (string?)null
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/demo/enable
    // ──────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Owner")]
    [HttpPost("enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Enable()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var mgmt = await GetOrCreateManagementAsync(tenantId.Value);

        if (mgmt.IsSandbox)
            return Ok(ApiResponse<object>.Ok(new { isSandbox = true, message = "Already in demo mode" }));

        mgmt.IsSandbox = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Demo mode ENABLED for tenant {TenantId}", tenantId);
        return Ok(ApiResponse<object>.Ok(new { isSandbox = true, message = "Demo mode enabled" }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/demo/disable
    // ──────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Owner")]
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Disable()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var mgmt = await GetOrCreateManagementAsync(tenantId.Value);

        if (!mgmt.IsSandbox)
            return Ok(ApiResponse<object>.Ok(new { isSandbox = false, message = "Already in production mode" }));

        mgmt.IsSandbox = false;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Demo mode DISABLED for tenant {TenantId}", tenantId);
        return Ok(ApiResponse<object>.Ok(new { isSandbox = false, message = "Demo mode disabled — now in production" }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/demo/seed
    // Seeds demo data (clients, bookings, services) for a sandbox tenant.
    // Restricted to Development and Staging environments to prevent accidental
    // test data insertion in production databases.
    // ──────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Owner")]
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SeedDemoData()
    {
        if (_env.IsProduction())
            return NotFound();

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var mgmt = await _db.TenantManagements
            .FirstOrDefaultAsync(m => m.TenantId == tenantId.Value);

        if (mgmt == null || !mgmt.IsSandbox)
            return BadRequest(ApiResponse<object>.Fail(
                "Demo seeding is only allowed in sandbox mode. Enable demo mode first."));

        // Check existing data to avoid double-seeding
        var hasData = await _db.Clients.AnyAsync(c => c.TenantId == tenantId.Value);
        if (hasData)
            return Ok(ApiResponse<object>.Ok(new { seeded = false, message = "Demo data already exists" }));

        var seededClients = 0;
        var seededServices = 0;

        try
        {
            // Seed 5 demo services
            var demoServices = new[]
            {
                ("Swedish Massage", "Wellness", 60, 85m, "#8B5CF6"),
                ("Deep Tissue Massage", "Wellness", 90, 120m, "#7C3AED"),
                ("Haircut & Style", "Hair", 45, 65m, "#EC4899"),
                ("Facial Treatment", "Skincare", 60, 95m, "#F59E0B"),
                // Was ("Yoga Session", "Fitness", …) — replaced rather than dropped so the demo
                // still seeds five services, in a vertical Upkilo actually serves.
                ("Manicure & Gel Polish", "Nails", 45, 55m, "#10B981"),
            };

            foreach (var (name, cat, dur, price, color) in demoServices)
            {
                _db.Services.Add(new Service
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    Name = name,
                    Category = cat,
                    Duration = dur,
                    Price = price,
                    Color = color,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                seededServices++;
            }

            // Seed 10 demo clients
            var firstNames = new[] { "Alice", "Bob", "Carol", "David", "Eva", "Frank", "Grace", "Henry", "Iris", "Jack" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Wilson", "Taylor" };

            for (int i = 0; i < 10; i++)
            {
                _db.Clients.Add(new Client
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    FirstName = firstNames[i],
                    LastName = lastNames[i],
                    Email = $"demo.{firstNames[i].ToLower()}@example.com",
                    Phone = $"+1555{i:D7}",
                    CreatedAt = DateTime.UtcNow.AddDays(-i * 7)
                });
                seededClients++;
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo seed failed for tenant {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<object>.Fail("Seed failed: " + ex.Message));
        }

        _logger.LogInformation(
            "Demo data seeded for tenant {TenantId}: {Clients} clients, {Services} services",
            tenantId, seededClients, seededServices);

        return Ok(ApiResponse<object>.Ok(new
        {
            seeded = true,
            clients = seededClients,
            services = seededServices,
            message = "Demo data seeded successfully"
        }));
    }
}
