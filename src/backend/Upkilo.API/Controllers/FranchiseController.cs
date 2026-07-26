using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Days 69-71: Franchise/Chain enterprise features.
/// Cross-location reporting, cross-location booking, HQ brand compliance push.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/franchise")]
[Authorize]
public class FranchiseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<FranchiseController> _logger;

    public FranchiseController(AppDbContext context, ITenantProvider tenantProvider, ILogger<FranchiseController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private List<Guid> GetSubTenantIds(Guid parentId)
        => _context.Tenants
            .Where(t => t.ParentTenantId == parentId && !t.IsDeleted)
            .Select(t => t.Id)
            .ToList();

    /// <summary>
    /// GET /api/v1/franchise/dashboard — Cross-location revenue + bookings in one view (agency owners only).
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetFranchiseDashboard([FromQuery] int days = 30)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var since = DateTime.UtcNow.AddDays(-days);
        var subIds = GetSubTenantIds(tenantId.Value);

        if (!subIds.Any())
            return Ok(ApiResponse<object>.Ok(new { message = "No sub-locations configured.", locations = Array.Empty<object>() }));

        var allIds = subIds.Concat(new[] { tenantId.Value }).ToList();

        var locationStats = await _context.Tenants
            .Where(t => allIds.Contains(t.Id))
            .Select(t => new
            {
                tenantId = t.Id,
                locationName = t.Name,
                t.City,
                revenue = _context.Bookings
                    .Where(b => b.TenantId == t.Id && b.Status == BookingStatus.Completed &&
                                b.PaymentStatus == PaymentStatus.Succeeded && b.StartTime >= since)
                    .Sum(b => (decimal?)b.Price ?? 0),
                bookingsCount = _context.Bookings
                    .Count(b => b.TenantId == t.Id && b.StartTime >= since && b.Status != BookingStatus.Cancelled),
                newClients = _context.Clients
                    .Count(c => c.TenantId == t.Id && c.CreatedAt >= since),
                activeStaff = _context.Staff
                    .Count(s => s.TenantId == t.Id && s.IsActive)
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            period = $"Last {days} days",
            summary = new
            {
                totalRevenue = locationStats.Sum(l => l.revenue),
                totalBookings = locationStats.Sum(l => l.bookingsCount),
                locationCount = locationStats.Count
            },
            locations = locationStats.OrderByDescending(l => l.revenue)
        }));
    }

    /// <summary>
    /// POST /api/v1/franchise/push-services — Day 71: HQ pushes services/pricing to all sub-locations.
    /// </summary>
    [HttpPost("push-services")]
    [Authorize(Roles = "Admin,AgencyOwner")]
    public async Task<IActionResult> PushServicesToLocations([FromBody] PushServicesRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var subIds = GetSubTenantIds(tenantId.Value);
        if (request.TargetLocationIds?.Any() == true)
            subIds = subIds.Intersect(request.TargetLocationIds).ToList();

        if (!subIds.Any())
            return BadRequest(ApiResponse.Fail("No target locations found."));

        var hqServices = await _context.Services
            .Where(s => s.TenantId == tenantId.Value && s.IsActive && !s.IsDeleted)
            .ToListAsync();

        int created = 0, updated = 0;

        foreach (var subId in subIds)
        {
            foreach (var hqSvc in hqServices)
            {
                var existing = await _context.Services
                    .FirstOrDefaultAsync(s => s.TenantId == subId && s.Name == hqSvc.Name && !s.IsDeleted);

                if (existing != null)
                {
                    if (request.OverridePricing)
                    {
                        existing.Price = hqSvc.Price;
                        existing.DurationMinutes = hqSvc.DurationMinutes;
                    }
                    existing.Description = hqSvc.Description;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    _context.Services.Add(new Service
                    {
                        Id = Guid.NewGuid(),
                        TenantId = subId,
                        Name = hqSvc.Name,
                        Description = hqSvc.Description,
                        Price = hqSvc.Price,
                        DurationMinutes = hqSvc.DurationMinutes,
                        IsActive = true,
                        Category = hqSvc.Category,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    created++;
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("[Franchise] Pushed {Services} services to {Locations} locations", hqServices.Count, subIds.Count);

        return Ok(ApiResponse<object>.Ok(new { locationsUpdated = subIds.Count, servicesCreated = created, servicesUpdated = updated }));
    }

    /// <summary>
    /// GET /api/v1/franchise/cross-location-availability — Day 70: Find availability across all franchise locations.
    /// </summary>
    [HttpGet("cross-location-availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCrossLocationAvailability(
        [FromQuery] Guid parentTenantId,
        [FromQuery] string? serviceCategory = null,
        [FromQuery] DateTime? preferredDate = null)
    {
        var date = preferredDate ?? DateTime.UtcNow.Date.AddDays(1);
        var dateEnd = date.AddDays(7);

        var subIds = await _context.Tenants
            .Where(t => t.ParentTenantId == parentTenantId && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync();
        subIds.Add(parentTenantId);

        var locations = await _context.Tenants
            .Where(t => subIds.Contains(t.Id))
            .Select(t => new
            {
                t.Id, t.Name, t.City,
                services = _context.Services
                    .Where(s => s.TenantId == t.Id && s.IsActive && !s.IsDeleted &&
                                (serviceCategory == null || s.Category == serviceCategory))
                    .Select(s => new { s.Id, s.Name, s.Price, s.DurationMinutes })
                    .ToList(),
                confirmedSlots = _context.Bookings
                    .Count(b => b.TenantId == t.Id && b.StartTime >= date && b.StartTime <= dateEnd &&
                                b.Status == BookingStatus.Confirmed)
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            searchDate = date,
            searchDateEnd = dateEnd,
            locations = locations.Where(l => l.services.Any()).OrderBy(l => l.City)
        }));
    }
}

public class PushServicesRequest
{
    public List<Guid>? TargetLocationIds { get; set; }
    public bool OverridePricing { get; set; } = true;
}
