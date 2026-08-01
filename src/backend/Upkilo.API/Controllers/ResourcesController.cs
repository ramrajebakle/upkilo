using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Resources controller for managing rooms, equipment, and other bookable resources.
/// Uses real database queries against Resource entity.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly ILogger<ResourcesController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ResourcesController(
        ILogger<ResourcesController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all resources
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetResources([FromQuery] string? type = null, [FromQuery] bool? isActive = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Resources
            .Where(r => r.TenantId == tenantId.Value && !r.IsDeleted);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(r => r.Type == type);
        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var resources = await query
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Type,
                r.Description,
                r.Capacity,
                r.Amenities,
                r.HourlyRate,
                r.IsActive,
                r.Color,
                r.LinkedServiceIds,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = resources });
    }

    /// <summary>
    /// Get resource by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetResource(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (resource == null) return NotFound();

        return Ok(new
        {
            resource.Id,
            resource.Name,
            resource.Type,
            resource.Description,
            resource.Capacity,
            resource.Amenities,
            resource.HourlyRate,
            resource.IsActive,
            resource.Color,
            resource.LinkedServiceIds,
            resource.CreatedAt,
            resource.UpdatedAt
        });
    }

    /// <summary>
    /// Create resource
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Resource name is required." });

        var amenitiesJson = request.Amenities != null && request.Amenities.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(request.Amenities)
            : null;

        var resource = new Resource
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Type = request.Type,
            Description = request.Description,
            Capacity = request.Capacity,
            Amenities = amenitiesJson,
            HourlyRate = request.HourlyRate,
            Color = request.Color,
            IsActive = true
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource created: {Id} - {Name}", resource.Id, resource.Name);

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, new
        {
            resource.Id,
            resource.Name,
            resource.CreatedAt
        });
    }

    /// <summary>
    /// Update resource
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(Guid id, [FromBody] UpdateResourceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (resource == null) return NotFound();

        if (request.Name != null) resource.Name = request.Name;
        if (request.Description != null) resource.Description = request.Description;
        if (request.Capacity.HasValue) resource.Capacity = request.Capacity.Value;
        if (request.HourlyRate.HasValue) resource.HourlyRate = request.HourlyRate.Value;
        if (request.IsActive.HasValue) resource.IsActive = request.IsActive.Value;
        if (request.Amenities != null)
            resource.Amenities = System.Text.Json.JsonSerializer.Serialize(request.Amenities);
        resource.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource updated: {ResourceId}", id);
        return Ok(new { success = true, resource.UpdatedAt });
    }

    /// <summary>
    /// Delete resource (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (resource == null) return NotFound();

        resource.IsDeleted = true;
        resource.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource deleted: {ResourceId}", id);
        return NoContent();
    }

    /// <summary>
    /// Link services to resource
    /// </summary>
    [HttpPost("{id}/services")]
    public async Task<IActionResult> LinkServices(Guid id, [FromBody] LinkServicesRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (resource == null) return NotFound();

        resource.LinkedServiceIds = System.Text.Json.JsonSerializer.Serialize(request.ServiceIds);
        resource.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Services linked to resource: {ResourceId}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Get resource types (static lookup)
    /// </summary>
    [HttpGet("types")]
    public IActionResult GetResourceTypes()
    {
        var types = new[]
        {
            new { type = "room", name = "Room", icon = "door" },
            new { type = "equipment", name = "Equipment", icon = "tool" },
            new { type = "vehicle", name = "Vehicle", icon = "car" },
            new { type = "other", name = "Other", icon = "box" }
        };

        return Ok(new { data = types });
    }

    // ─── Advanced Resource Scheduling ───

    /// <summary>
    /// Check resource availability for a date range
    /// </summary>
    [HttpGet("{id}/availability")]
    public async Task<IActionResult> GetAvailability(
        Guid id,
        [FromQuery] DateTime date,
        [FromQuery] int days = 1)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (resource == null) return NotFound();

        var startDate = date.Date;
        var endDate = startDate.AddDays(days);

        var bookings = await _context.ResourceBookings
            .Where(b => b.ResourceId == id
                && b.TenantId == tenantId.Value
                && !b.IsDeleted
                && b.Status != "cancelled"
                && b.StartTime < endDate
                && b.EndTime > startDate)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.StartTime,
                b.EndTime,
                b.Status,
                b.BookedByUserId,
                b.Notes
            })
            .ToListAsync();

        // Generate availability slots (hourly blocks from 8AM to 8PM)
        var slots = new List<object>();
        for (var d = startDate; d < endDate; d = d.AddDays(1))
        {
            for (var hour = 8; hour < 20; hour++)
            {
                var slotStart = d.AddHours(hour);
                var slotEnd = slotStart.AddHours(1);
                var isBooked = bookings.Any(b => b.StartTime < slotEnd && b.EndTime > slotStart);
                slots.Add(new
                {
                    start = slotStart,
                    end = slotEnd,
                    available = !isBooked,
                    booking = isBooked ? bookings.FirstOrDefault(b => b.StartTime < slotEnd && b.EndTime > slotStart) : null
                });
            }
        }

        return Ok(new
        {
            resourceId = id,
            resourceName = resource.Name,
            date = startDate,
            days,
            bookings,
            slots
        });
    }

    /// <summary>
    /// Book a resource time slot (with conflict detection)
    /// </summary>
    [HttpPost("{id}/book")]
    public async Task<IActionResult> BookResource(Guid id, [FromBody] BookResourceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value && !r.IsDeleted);

        if (resource == null) return NotFound();

        if (!resource.IsActive)
            return BadRequest(new { error = "This resource is currently inactive." });

        if (request.StartTime >= request.EndTime)
            return BadRequest(new { error = "End time must be after start time." });

        // Conflict detection
        var hasConflict = await _context.ResourceBookings
            .AnyAsync(b => b.ResourceId == id
                && b.TenantId == tenantId.Value
                && !b.IsDeleted
                && b.Status != "cancelled"
                && b.StartTime < request.EndTime
                && b.EndTime > request.StartTime);

        if (hasConflict)
            return Conflict(new { error = "This resource is already booked for the requested time slot." });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var booking = new Upkilo.Core.Entities.ResourceBooking
        {
            TenantId = tenantId.Value,
            ResourceId = id,
            BookingId = request.BookingId,
            Title = request.Title ?? $"{resource.Name} Booking",
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = "confirmed",
            BookedByUserId = Guid.TryParse(userId, out var uid) ? uid : null,
            Notes = request.Notes
        };

        _context.ResourceBookings.Add(booking);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource booked: {ResourceId} from {Start} to {End}", id, request.StartTime, request.EndTime);

        return CreatedAtAction(nameof(GetResource), new { id }, new
        {
            bookingId = booking.Id,
            booking.ResourceId,
            booking.Title,
            booking.StartTime,
            booking.EndTime,
            booking.Status
        });
    }

    /// <summary>
    /// Get bookings for a specific resource
    /// </summary>
    [HttpGet("{id}/bookings")]
    public async Task<IActionResult> GetResourceBookings(
        Guid id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var startDate = from ?? DateTime.UtcNow.Date;
        var endDate = to ?? startDate.AddDays(30);

        var bookings = await _context.ResourceBookings
            .Where(b => b.ResourceId == id
                && b.TenantId == tenantId.Value
                && !b.IsDeleted
                && b.StartTime < endDate
                && b.EndTime > startDate)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.ResourceId,
                b.BookingId,
                b.Title,
                b.StartTime,
                b.EndTime,
                b.Status,
                b.BookedByUserId,
                b.Notes,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = bookings });
    }

    /// <summary>
    /// Cancel a resource booking
    /// </summary>
    [HttpDelete("{id}/bookings/{bookingId}")]
    public async Task<IActionResult> CancelResourceBooking(Guid id, Guid bookingId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var booking = await _context.ResourceBookings
            .FirstOrDefaultAsync(b => b.Id == bookingId
                && b.ResourceId == id
                && b.TenantId == tenantId.Value
                && !b.IsDeleted);

        if (booking == null) return NotFound();

        booking.Status = "cancelled";
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource booking cancelled: {BookingId}", bookingId);
        return NoContent();
    }

    /// <summary>
    /// Multi-resource schedule view — get all bookings across multiple resources
    /// </summary>
    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? resourceIds = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var targetDate = date?.Date ?? DateTime.UtcNow.Date;
        var endDate = targetDate.AddDays(1);

        var query = _context.ResourceBookings
            .Where(b => b.TenantId == tenantId.Value
                && !b.IsDeleted
                && b.Status != "cancelled"
                && b.StartTime < endDate
                && b.EndTime > targetDate);

        if (!string.IsNullOrEmpty(resourceIds))
        {
            var ids = resourceIds.Split(',')
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();

            if (ids.Count > 0)
                query = query.Where(b => ids.Contains(b.ResourceId));
        }

        var bookings = await query
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.ResourceId,
                b.Title,
                b.StartTime,
                b.EndTime,
                b.Status,
                b.BookedByUserId
            })
            .ToListAsync();

        var resources = await _context.Resources
            .Where(r => r.TenantId == tenantId.Value && !r.IsDeleted && r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.Type, r.Color, r.Capacity })
            .ToListAsync();

        return Ok(new
        {
            date = targetDate,
            resources,
            bookings
        });
    }
}

// Request DTOs
public class CreateResourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "room";
    public string? Description { get; set; }
    public int Capacity { get; set; } = 1;
    public List<string>? Amenities { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Color { get; set; }
}

public class UpdateResourceRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public List<string>? Amenities { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool? IsActive { get; set; }
}

public class LinkServicesRequest
{
    public List<Guid> ServiceIds { get; set; } = new();
}

public class BookResourceRequest
{
    public string? Title { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid? BookingId { get; set; }
    public string? Notes { get; set; }
}


