using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Waitlist controller for managing booking waitlists.
/// Uses real database queries against WaitlistEntries.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly ILogger<WaitlistController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public WaitlistController(
        ILogger<WaitlistController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEventService eventService,
        IBookingService bookingService)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _eventService = eventService;
        _bookingService = bookingService;
    }

    /// <summary>
    /// Get waitlist entries with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWaitlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? serviceId = null,
        [FromQuery] Guid? staffId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? converted = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.WaitlistEntries
            .Where(w => w.TenantId == tenantId.Value);

        if (serviceId.HasValue)
            query = query.Where(w => w.ServiceId == serviceId.Value);

        if (staffId.HasValue)
            query = query.Where(w => w.StaffId == staffId.Value);

        if (startDate.HasValue)
            query = query.Where(w => w.PreferredDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.PreferredDate <= endDate.Value);

        if (converted.HasValue)
            query = query.Where(w => w.IsConverted == converted.Value);

        var total = await query.CountAsync();

        var entries = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.Id,
                w.ServiceId,
                w.ClientId,
                w.Email,
                w.FirstName,
                w.LastName,
                w.Phone,
                w.PreferredDate,
                w.PreferredTimeRange,
                w.Notes,
                w.IsConverted,
                w.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = entries, total, page, pageSize });
    }

    /// <summary>
    /// Get waitlist entry by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWaitlistEntry(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        return Ok(new
        {
            entry.Id,
            entry.ServiceId,
            entry.ClientId,
            entry.Email,
            entry.FirstName,
            entry.LastName,
            entry.Phone,
            entry.PreferredDate,
            entry.PreferredTimeRange,
            entry.Notes,
            entry.IsConverted,
            entry.CreatedAt
        });
    }

    /// <summary>
    /// Add to waitlist (admin)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToWaitlist([FromBody] AddToWaitlistRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required." });

        // Verify service belongs to tenant
        var serviceExists = await _context.Services.AnyAsync(s => s.Id == request.ServiceId && s.TenantId == tenantId);
        if (!serviceExists) return BadRequest("Invalid service.");

        if (request.ClientId.HasValue)
        {
            var clientExists = await _context.Clients.AnyAsync(c => c.Id == request.ClientId && c.TenantId == tenantId);
            if (!clientExists) return BadRequest("Invalid client.");
        }

        var entry = new WaitlistEntry
        {
            TenantId = tenantId.Value,
            ServiceId = request.ServiceId,
            ClientId = request.ClientId,
            StaffId = request.StaffId,
            Email = request.Email,
            FirstName = request.FirstName ?? "",
            LastName = request.LastName ?? "",
            Phone = request.Phone,
            PreferredDate = request.PreferredDate,
            PreferredTimeRange = request.PreferredTimeRange,
            Notes = request.Notes
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added to waitlist: {Email} for service {ServiceId}", entry.Email, entry.ServiceId);

        return CreatedAtAction(nameof(GetWaitlistEntry), new { id = entry.Id }, new
        {
            entry.Id,
            entry.Email,
            entry.ServiceId,
            entry.PreferredDate,
            entry.CreatedAt
        });
    }

    /// <summary>
    /// Add to waitlist (public/client-facing)
    /// </summary>
    [HttpPost("public")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicAddToWaitlist([FromBody] PublicWaitlistRequest request)
    {
        if (request.TenantId == Guid.Empty)
            return BadRequest(new { error = "Tenant ID is required." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required." });

        // Verify service belongs to tenant
        var serviceExists = await _context.Services.AnyAsync(s => s.Id == request.ServiceId && s.TenantId == request.TenantId);
        if (!serviceExists) return BadRequest("Invalid service.");

        var entry = new WaitlistEntry
        {
            TenantId = request.TenantId,
            ServiceId = request.ServiceId,
            Email = request.Email,
            FirstName = request.FirstName ?? "",
            LastName = request.LastName ?? "",
            Phone = request.Phone,
            PreferredDate = request.PreferredDate,
            PreferredTimeRange = request.PreferredTimeRange,
            Notes = request.Notes
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Public waitlist signup: {Email} for service {ServiceId}", entry.Email, entry.ServiceId);

        await _eventService.PublishAsync("waitlist.added", new { entry.Id, entry.Email, entry.ServiceId }, request.TenantId);

        return Ok(new
        {
            success = true,
            message = "You've been added to the waitlist! We'll notify you when a spot opens up.",
            entry.Id
        });
    }

    /// <summary>
    /// Update waitlist entry
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWaitlistEntry(Guid id, [FromBody] UpdateWaitlistRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        if (request.ServiceId.HasValue)
        {
            var serviceExists = await _context.Services.AnyAsync(s => s.Id == request.ServiceId.Value && s.TenantId == tenantId);
            if (!serviceExists) return BadRequest("Invalid service.");
            entry.ServiceId = request.ServiceId.Value;
        }

        if (request.PreferredDate.HasValue) entry.PreferredDate = request.PreferredDate.Value;
        if (request.PreferredTimeRange != null) entry.PreferredTimeRange = request.PreferredTimeRange;
        if (request.Notes != null) entry.Notes = request.Notes;
        if (request.Priority.HasValue) entry.Priority = request.Priority.Value;
        if (request.StaffId.HasValue) entry.StaffId = request.StaffId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Bulk notify clients of availability
    /// </summary>
    [HttpPost("bulk-notify")]
    public async Task<IActionResult> BulkNotify([FromBody] BulkNotifyRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entries = await _context.WaitlistEntries
            .Where(w => request.Ids.Contains(w.Id) && w.TenantId == tenantId.Value)
            .ToListAsync();

        foreach (var entry in entries)
        {
            await _eventService.PublishAsync("waitlist.availability", new
            {
                entry.Id,
                entry.Email,
                entry.FirstName,
                entry.ServiceId,
                availableSlots = request.AvailableSlots
            }, tenantId.Value);

            entry.Status = WaitlistStatus.Notified;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Bulk availability notification sent to {Count} entries", entries.Count);

        return Ok(new { success = true, notifiedCount = entries.Count });
    }

    /// <summary>
    /// Bulk remove from waitlist
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entries = await _context.WaitlistEntries
            .Where(w => ids.Contains(w.Id) && w.TenantId == tenantId.Value)
            .ToListAsync();

        _context.WaitlistEntries.RemoveRange(entries);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk removed {Count} entries from waitlist", entries.Count);
        return Ok(new { success = true, deletedCount = entries.Count });
    }



    /// <summary>
    /// Convert waitlist entry to active booking
    /// </summary>
    [HttpPost("{id}/convert")]
    public async Task<IActionResult> ConvertToBooking(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .Include(w => w.Client)
            .Include(w => w.Service)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        if (entry == null) return NotFound();

        var bookingModel = new CreateBookingModel(
            ClientId: entry.ClientId,
            ServiceId: entry.ServiceId,
            StaffId: entry.StaffId ?? Guid.Empty,
            StartTime: entry.PreferredDate,
            EndTime: entry.PreferredDate.AddMinutes(entry.Service?.DurationMinutes ?? 60),
            Notes: $"Converted from waitlist entry {id}. " + entry.Notes
        );

        try
        {
            var booking = await _bookingService.CreateBookingAsync(tenantId.Value, bookingModel);

            // 2. Mark waitlist entry as converted
            entry.Status = WaitlistStatus.Converted;
            entry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Waitlist entry {EntryId} converted to booking {BookingId}", id, booking.Id);

            return Ok(new
            {
                success = true,
                bookingId = booking.Id,
                message = "Waitlist entry successfully converted to a confirmed booking."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    /// <summary>
    /// Remove from waitlist
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromWaitlist(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        _context.WaitlistEntries.Remove(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed from waitlist: {Email}", entry.Email);
        return NoContent();
    }

    /// <summary>
    /// Notify client of availability
    /// </summary>
    [HttpPost("{id}/notify")]
    public async Task<IActionResult> NotifyClient(Guid id, [FromBody] NotifyAvailabilityRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        await _eventService.PublishAsync("waitlist.availability", new
        {
            entry.Id,
            entry.Email,
            entry.FirstName,
            entry.ServiceId,
            availableSlots = request.AvailableSlots
        }, tenantId.Value);

        _logger.LogInformation("Availability notification sent to {Email} for waitlist {Id}", entry.Email, id);

        return Ok(new { success = true, message = $"Notification sent to {entry.Email}" });
    }

    /// <summary>
    /// Update waitlist entry status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        entry.Status = request.Status;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Update waitlist entry priority
    /// </summary>
    [HttpPatch("{id}/priority")]
    public async Task<IActionResult> UpdatePriority(Guid id, [FromQuery] int priority)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        entry.Priority = priority;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Get waitlist statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var stats = await _context.WaitlistEntries
            .Where(w => w.TenantId == tenantId.Value)
            .GroupBy(w => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Waiting = g.Count(w => w.Status == WaitlistStatus.Waiting),
                Notified = g.Count(w => w.Status == WaitlistStatus.Notified),
                Converted = g.Count(w => w.Status == WaitlistStatus.Converted),
                Cancelled = g.Count(w => w.Status == WaitlistStatus.Cancelled)
            })
            .FirstOrDefaultAsync();

        return Ok(stats ?? new { Total = 0, Waiting = 0, Notified = 0, Converted = 0, Cancelled = 0 });
    }

    /// <summary>
    /// Bulk update waitlist priority (reorder)
    /// </summary>
    [HttpPost("bulk-priority")]
    public async Task<IActionResult> BulkUpdatePriority([FromBody] List<WaitlistPriorityUpdate> updates)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var ids = updates.Select(u => u.Id).ToList();
        var entries = await _context.WaitlistEntries
            .Where(w => ids.Contains(w.Id) && w.TenantId == tenantId.Value)
            .ToListAsync();

        foreach (var entry in entries)
        {
            var update = updates.First(u => u.Id == entry.Id);
            entry.Priority = update.Priority;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, updatedCount = entries.Count });
    }

    /// <summary>
    /// Export waitlist to CSV
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportToCsv()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entries = await _context.WaitlistEntries
            .Where(w => w.TenantId == tenantId.Value)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .Select(w => new
            {
                w.FirstName,
                w.LastName,
                w.Email,
                w.Phone,
                w.Status,
                w.PreferredDate,
                w.CreatedAt
            })
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("FirstName,LastName,Email,Phone,Status,PreferredDate,CreatedAt");

        foreach (var entry in entries)
        {
            csv.AppendLine($"{entry.FirstName},{entry.LastName},{entry.Email},{entry.Phone},{entry.Status},{entry.PreferredDate:yyyy-MM-dd},{entry.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"waitlist_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Get waitlist position for a specific entry
    /// </summary>
    [HttpGet("{id}/position")]
    public async Task<IActionResult> GetWaitlistPosition(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId.Value);

        if (entry == null) return NotFound();

        // Position is determined by Priority (desc) then CreatedAt (asc)
        var position = await _context.WaitlistEntries
            .Where(w => w.TenantId == tenantId.Value
                        && w.Status == WaitlistStatus.Waiting
                        && (w.Priority > entry.Priority
                            || (w.Priority == entry.Priority && w.CreatedAt < entry.CreatedAt)))
            .CountAsync() + 1;

        var totalWaiting = await _context.WaitlistEntries
            .CountAsync(w => w.TenantId == tenantId.Value && w.Status == WaitlistStatus.Waiting);

        return Ok(new { position, totalWaiting });
    }
}

// Request DTOs
public class AddToWaitlistRequest
{
    public Guid ServiceId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? StaffId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? PreferredTimeRange { get; set; }
    public string? Notes { get; set; }
}

public class PublicWaitlistRequest
{
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? PreferredTimeRange { get; set; }
    public string? Notes { get; set; }
}

public class UpdateWaitlistRequest
{
    public Guid? ServiceId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime? PreferredDate { get; set; }
    public string? PreferredTimeRange { get; set; }
    public string? Notes { get; set; }
    public int? Priority { get; set; }
}

public class NotifyAvailabilityRequest
{
    public List<AvailableSlot>? AvailableSlots { get; set; }
    public string? CustomMessage { get; set; }
}

public class BulkNotifyRequest : NotifyAvailabilityRequest
{
    public List<Guid> Ids { get; set; } = new();
}

public class AvailableSlot
{
    public DateTime Date { get; set; }
    public string? TimeSlot { get; set; }
    public Guid? StaffId { get; set; }
}

public class ConvertToBookingRequest
{
    public DateTime BookingDate { get; set; }
    public Guid? StaffId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateStatusRequest
{
    public WaitlistStatus Status { get; set; }
}

public class WaitlistPriorityUpdate
{
    public Guid Id { get; set; }
    public int Priority { get; set; }
}

