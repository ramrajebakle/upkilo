using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Upsell controller for service addon suggestions
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UpsellController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UpsellController> _logger;

    public UpsellController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<UpsellController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get recommended addons for a service
    /// </summary>
    [HttpGet("service/{serviceId}/addons")]
    public async Task<IActionResult> GetServiceAddons(Guid serviceId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId);

        if (service == null) return NotFound();

        // Get services in the same category that could be addons
        // (shorter duration, lower price = likely addon)
        var addons = await _context.Services
            .Where(s => s.TenantId == tenantId &&
                        s.Id != serviceId &&
                        s.IsActive &&
                        s.DurationMinutes <= 30 &&  // Addons are typically quick
                        s.Price <= service.Price * 0.5m) // Cheaper than main service
            .OrderBy(s => s.Price)
            .Take(5)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.Price,
                Duration = s.DurationMinutes,
                SuggestedReason = "Popular addon"
            })
            .ToListAsync();

        return Ok(new { data = addons });
    }

    /// <summary>
    /// Get personalized upsell recommendations for a client
    /// </summary>
    [HttpGet("client/{clientId}/recommendations")]
    public async Task<IActionResult> GetClientRecommendations(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId);

        if (client == null) return NotFound();

        // Get services the client has booked before
        var bookedServiceIds = await _context.Bookings
            .Where(b => b.ClientId == clientId && b.ServiceId.HasValue)
            .Select(b => b.ServiceId!.Value)
            .Distinct()
            .ToListAsync();

        // Get services they haven't tried yet
        var unbookedServices = await _context.Services
            .Where(s => s.TenantId == tenantId &&
                        s.IsActive &&
                        !bookedServiceIds.Contains(s.Id))
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.Price,
                Duration = s.DurationMinutes,
                SuggestedReason = "You haven't tried this yet"
            })
            .ToListAsync();

        // Get popular services based on booking count
        var popularServices = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.ServiceId.HasValue &&
                        b.CreatedAt >= DateTime.UtcNow.AddDays(-90))
            .GroupBy(b => b.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToListAsync();

        var popularServiceIds = popularServices.Select(x => x.ServiceId).ToList();
        var popularDetails = await _context.Services
            .Where(s => popularServiceIds.Contains(s.Id) && !bookedServiceIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.Price,
                Duration = s.DurationMinutes,
                SuggestedReason = "Popular with other clients"
            })
            .ToListAsync();

        // Combine recommendations
        var recommendations = unbookedServices.Take(3)
            .Concat(popularDetails)
            .DistinctBy(x => x.Id)
            .Take(5)
            .ToList();

        return Ok(new
        {
            clientName = client.FullName,
            loyaltyTier = client.LoyaltyTier,
            recommendations
        });
    }

    /// <summary>
    /// Get upgrade suggestions for a booked service
    /// </summary>
    [HttpGet("booking/{bookingId}/upgrades")]
    public async Task<IActionResult> GetBookingUpgrades(Guid bookingId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var booking = await _context.Bookings
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);

        if (booking?.Service == null) return NotFound();

        // Find more premium versions of the same category
        var currentPrice = booking.Service.Price;
        var currentDuration = booking.Service.DurationMinutes;

        var upgrades = await _context.Services
            .Where(s => s.TenantId == tenantId &&
                        s.Id != booking.ServiceId &&
                        s.IsActive &&
                        s.Price > currentPrice &&
                        s.Price <= currentPrice * 2) // Up to 2x the price
            .OrderBy(s => s.Price)
            .Take(3)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.Price,
                Duration = s.DurationMinutes,
                PriceDifference = s.Price - currentPrice,
                SuggestedReason = s.DurationMinutes > currentDuration
                    ? $"+{s.DurationMinutes - currentDuration} mins more"
                    : "Premium experience"
            })
            .ToListAsync();

        return Ok(new
        {
            currentService = booking.Service.Name,
            upgrades
        });
    }

    /// <summary>
    /// Get downtime filler recommendations (services that fit in gaps)
    /// </summary>
    [HttpGet("staff/{staffId}/gaps")]
    public async Task<IActionResult> GetDowntimeFillers(Guid staffId, [FromQuery] DateTime date)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Simple logic: Find short services (< 45 min) that could fill gaps
        var shortServices = await _context.Services
            .Where(s => s.TenantId == tenantId && s.IsActive && s.DurationMinutes <= 45)
            .OrderBy(s => s.DurationMinutes)
            .Take(3)
            .Select(s => new { s.Id, s.Name, s.DurationMinutes, s.Price, Reason = "Fits in your gap" })
            .ToListAsync();

        return Ok(new { data = shortServices });
    }

    /// <summary>
    /// Suggest membership upgrades based on client behavior
    /// </summary>
    [HttpGet("client/{clientId}/membership-upsell")]
    public async Task<IActionResult> GetMembershipUpsell(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var bookingCount = await _context.Bookings.CountAsync(b => b.ClientId == clientId && b.TenantId == tenantId);

        if (bookingCount > 3)
        {
            var plans = await _context.MembershipPlans
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .OrderBy(p => p.Price)
                .Take(2)
                .Select(p => new { p.Id, p.Name, p.Price, Reason = "Frequent visitor discount" })
                .ToListAsync();

            return Ok(new { data = plans });
        }

        return Ok(new { data = new List<object>() });
    }

    /// <summary>
    /// Suggest service packages for bundle savings
    /// </summary>
    [HttpGet("client/{clientId}/package-upsell")]
    public async Task<IActionResult> GetPackageUpsell(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var packages = await _context.ServicePackages
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .Select(p => new { p.Id, p.Name, p.TotalPrice, Reason = "Bundle and save 20%" })
            .ToListAsync();

        return Ok(new { data = packages });
    }
}

