using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Schedule controller — staff availability, slot management, and calendar views.
/// Uses WorkingHours, ScheduleException, SlotHold, and Booking entities.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly ILogger<ScheduleController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ScheduleController(
        ILogger<ScheduleController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get staff schedule for a date range
    /// </summary>
    [HttpGet("staff/{staffId}")]
    public async Task<IActionResult> GetStaffSchedule(
        Guid staffId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Working hours template
        var workingHours = await _context.StaffWorkingHours
            .Where(wh => wh.StaffId == staffId && wh.TenantId == tenantId.Value && !wh.IsDeleted)
            .OrderBy(wh => wh.DayOfWeek)
            .Select(wh => new
            {
                wh.DayOfWeek,
                wh.IsWorkingDay,
                startTime = wh.IsWorkingDay ? wh.StartTime.ToString(@"hh\:mm") : null,
                endTime = wh.IsWorkingDay ? wh.EndTime.ToString(@"hh\:mm") : null
            })
            .ToListAsync();

        // Schedule exceptions in date range
        var startOnly = DateOnly.FromDateTime(startDate);
        var endOnly = DateOnly.FromDateTime(endDate);
        var exceptions = await _context.StaffExceptions
            .Where(se => se.StaffId == staffId && se.TenantId == tenantId.Value && !se.IsDeleted &&
                se.Date >= startOnly && se.Date <= endOnly)
            .Select(se => new
            {
                se.Id,
                date = se.Date.ToString("yyyy-MM-dd"),
                se.Type,
                se.IsAllDay,
                startTime = se.StartTime.HasValue ? se.StartTime.Value.ToString(@"hh\:mm") : (string?)null,
                endTime = se.EndTime.HasValue ? se.EndTime.Value.ToString(@"hh\:mm") : (string?)null,
                se.Reason
            })
            .ToListAsync();

        // Bookings in date range
        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.StaffId == staffId && b.TenantId == tenantId.Value && !b.IsDeleted &&
                b.StartTime >= startDate && b.StartTime <= endDate)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                date = b.StartTime.ToString("yyyy-MM-dd"),
                time = b.StartTime.ToString("HH:mm"),
                duration = b.Service != null ? b.Service.DurationMinutes : 0,
                clientName = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Walk-in",
                serviceName = b.Service != null ? b.Service.Name : "Unknown",
                status = b.Status.ToString().ToLower()
            })
            .ToListAsync();

        return Ok(new { staffId, startDate = startDate.Date, endDate = endDate.Date, workingHours, exceptions, bookings });
    }

    /// <summary>
    /// Update staff working hours
    /// </summary>
    [HttpPut("staff/{staffId}/working-hours")]
    public async Task<IActionResult> UpdateStaffWorkingHours(Guid staffId, [FromBody] List<WorkingHoursRequest> hours)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Remove existing working hours for this staff
        var existing = await _context.StaffWorkingHours
            .Where(wh => wh.StaffId == staffId && wh.TenantId == tenantId.Value)
            .ToListAsync();
        _context.StaffWorkingHours.RemoveRange(existing);

        // Add new working hours
        foreach (var h in hours)
        {
            _context.StaffWorkingHours.Add(new WorkingHours
            {
                TenantId = tenantId.Value,
                StaffId = staffId,
                DayOfWeek = h.DayOfWeek,
                IsWorkingDay = h.IsWorkingDay,
                StartTime = TimeSpan.TryParse(h.StartTime, out var st) ? st : TimeSpan.Zero,
                EndTime = TimeSpan.TryParse(h.EndTime, out var et) ? et : TimeSpan.Zero,
                BreakStartTime = TimeSpan.TryParse(h.BreakStartTime, out var bst) ? bst : null,
                BreakEndTime = TimeSpan.TryParse(h.BreakEndTime, out var bet) ? bet : null
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Working hours updated for staff {StaffId}", staffId);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Add schedule exception (time off, vacation, etc.)
    /// </summary>
    [HttpPost("staff/{staffId}/exceptions")]
    public async Task<IActionResult> AddScheduleException(Guid staffId, [FromBody] ScheduleExceptionRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!DateOnly.TryParse(request.Date, out var date))
            return BadRequest(new { error = "Invalid date." });

        var exception = new ScheduleException
        {
            TenantId = tenantId.Value,
            StaffId = staffId,
            Date = date,
            Type = request.Type,
            IsAllDay = request.IsAllDay,
            StartTime = TimeSpan.TryParse(request.StartTime, out var st) ? st : null,
            EndTime = TimeSpan.TryParse(request.EndTime, out var et) ? et : null,
            Reason = request.Reason
        };

        _context.StaffExceptions.Add(exception);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule exception added for staff {StaffId}: {Type} on {Date}", staffId, request.Type, request.Date);

        return CreatedAtAction(nameof(GetScheduleException), new { staffId, id = exception.Id }, new
        {
            exception.Id,
            staffId,
            date = request.Date,
            type = request.Type,
            isAllDay = request.IsAllDay,
            startTime = request.StartTime,
            endTime = request.EndTime,
            reason = request.Reason,
            createdAt = exception.CreatedAt
        });
    }

    /// <summary>
    /// Get a specific schedule exception
    /// </summary>
    [HttpGet("staff/{staffId}/exceptions/{id}")]
    public async Task<IActionResult> GetScheduleException(Guid staffId, Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var exception = await _context.StaffExceptions
            .FirstOrDefaultAsync(se => se.Id == id && se.StaffId == staffId && se.TenantId == tenantId.Value && !se.IsDeleted);

        if (exception == null) return NotFound();

        return Ok(new
        {
            exception.Id,
            staffId,
            date = exception.Date.ToString("yyyy-MM-dd"),
            exception.Type,
            exception.IsAllDay,
            startTime = exception.StartTime?.ToString(@"hh\:mm"),
            endTime = exception.EndTime?.ToString(@"hh\:mm"),
            exception.Reason
        });
    }

    /// <summary>
    /// Delete a schedule exception
    /// </summary>
    [HttpDelete("staff/{staffId}/exceptions/{id}")]
    public async Task<IActionResult> DeleteScheduleException(Guid staffId, Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var exception = await _context.StaffExceptions
            .FirstOrDefaultAsync(se => se.Id == id && se.StaffId == staffId && se.TenantId == tenantId.Value && !se.IsDeleted);

        if (exception == null) return NotFound();

        exception.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Schedule exception deleted: {ExceptionId} for staff {StaffId}", id, staffId);
        return NoContent();
    }

    /// <summary>
    /// Get available slots for a service/staff on a date.
    /// Computes availability by subtracting bookings and exceptions from working hours.
    /// </summary>
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid serviceId,
        [FromQuery] Guid? staffId,
        [FromQuery] DateTime date,
        [FromQuery] Guid? locationId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
 
        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == serviceId);
        if (service == null) return BadRequest(new { error = "Service not found." });

        var dayOfWeek = (int)date.DayOfWeek;
        var dateOnly = DateOnly.FromDateTime(date);

        // Get staff to check
        var staffQuery = _context.StaffMembers
            .Where(s => s.TenantId == tenantId.Value && !s.IsDeleted && s.IsActive);

        if (staffId.HasValue)
            staffQuery = staffQuery.Where(s => s.Id == staffId.Value);

        var staffList = await staffQuery.Select(s => new { s.Id, Name = $"{s.FirstName} {s.LastName}" }).ToListAsync();

        var slots = new List<object>();

        foreach (var staff in staffList)
        {
            // Check if exception blocks this day
            var hasAllDayException = await _context.StaffExceptions
                .AnyAsync(se => se.StaffId == staff.Id && se.TenantId == tenantId.Value &&
                    se.Date == dateOnly && se.IsAllDay && !se.IsDeleted);

            if (hasAllDayException) continue;

            // Get working hours for this day
            var wh = await _context.StaffWorkingHours
                .FirstOrDefaultAsync(w => w.StaffId == staff.Id && w.TenantId == tenantId.Value &&
                    w.DayOfWeek == dayOfWeek && w.IsWorkingDay && !w.IsDeleted);

            if (wh == null) continue;

            // Get existing bookings on this day for this staff
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);
            var existingBookings = await _context.Bookings
                .Where(b => b.StaffId == staff.Id && b.TenantId == tenantId.Value && !b.IsDeleted &&
                    b.StartTime >= dayStart && b.StartTime < dayEnd &&
                    b.Status != BookingStatus.Cancelled)
                .Select(b => new { b.StartTime, b.EndTime })
                .ToListAsync();

            // Get active slot holds
            var activeHolds = await _context.SlotHolds
                .Where(h => h.StaffId == staff.Id && h.TenantId == tenantId.Value &&
                    h.SlotDateTime >= dayStart && h.SlotDateTime < dayEnd &&
                    !h.IsReleased && h.ExpiresAt > DateTime.UtcNow)
                .Select(h => new { StartTime = h.SlotDateTime, EndTime = h.SlotDateTime.AddMinutes(h.DurationMinutes) })
                .ToListAsync();

            // Generate available slots
            var interval = TimeSpan.FromMinutes(30);
            var serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);
            var currentTime = wh.StartTime;

            while (currentTime + serviceDuration <= wh.EndTime)
            {
                var slotStart = date.Date.Add(currentTime);
                var slotEnd = slotStart.Add(serviceDuration);

                // Check if slot conflicts with existing booking or hold
                var isBooked = existingBookings.Any(b => b.StartTime < slotEnd && b.EndTime > slotStart);
                var isHeld = activeHolds.Any(h => h.StartTime < slotEnd && h.EndTime > slotStart);

                // Check break time
                var inBreak = wh.BreakStartTime.HasValue && wh.BreakEndTime.HasValue &&
                    currentTime < wh.BreakEndTime.Value && currentTime + serviceDuration > wh.BreakStartTime.Value;

                slots.Add(new
                {
                    time = currentTime.ToString(@"hh\:mm"),
                    available = !isBooked && !isHeld && !inBreak,
                    staffName = staffId == null ? staff.Name : (string?)null,
                    staffId = staff.Id
                });

                currentTime = currentTime.Add(interval);
            }
        }

        return Ok(new { date = date.Date, serviceId, staffId, locationId, slots });
    }

    /// <summary>
    /// Hold a slot temporarily during booking
    /// </summary>
    [HttpPost("hold")]
    public async Task<IActionResult> HoldSlot([FromBody] HoldSlotRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var sessionToken = Guid.NewGuid().ToString("N");

        var hold = new SlotHold
        {
            TenantId = tenantId.Value,
            StaffId = request.StaffId ?? Guid.Empty,
            ServiceId = request.ServiceId,
            SlotDateTime = request.SlotDateTime,
            DurationMinutes = request.DurationMinutes,
            SessionToken = sessionToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _context.SlotHolds.Add(hold);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Slot held: {HoldId} for {DateTime}", hold.Id, request.SlotDateTime);

        return Ok(new
        {
            holdId = hold.Id,
            sessionToken,
            expiresAt = hold.ExpiresAt,
            slotDateTime = request.SlotDateTime
        });
    }

    /// <summary>
    /// Release a held slot
    /// </summary>
    [HttpDelete("hold/{holdId}")]
    public async Task<IActionResult> ReleaseHold(Guid holdId, [FromQuery] string sessionToken)
    {
        var hold = await _context.SlotHolds
            .FirstOrDefaultAsync(h => h.Id == holdId && h.SessionToken == sessionToken && !h.IsReleased);

        if (hold == null) return NotFound();

        hold.IsReleased = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Slot hold released: {HoldId}", holdId);
        return NoContent();
    }

    /// <summary>
    /// Get calendar view for all staff
    /// </summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarView(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? locationId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Get bookings as calendar events
        var bookingEvents = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.TenantId == tenantId.Value && !b.IsDeleted &&
                b.StartTime >= startDate && b.StartTime <= endDate &&
                (locationId == null || b.LocationId == locationId))
            .Select(b => new
            {
                b.Id,
                title = $"{(b.Service != null ? b.Service.Name : "Booking")} - {(b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Walk-in")}",
                staffId = b.StaffId,
                staffName = b.Staff != null ? $"{b.Staff.FirstName} {b.Staff.LastName}" : "Unassigned",
                start = b.StartTime.ToString("o"),
                end = b.EndTime.ToString("o"),
                color = "#3B82F6",
                type = "booking"
            })
            .ToListAsync();

        // Get schedule exceptions as events
        var startOnly = DateOnly.FromDateTime(startDate);
        var endOnly = DateOnly.FromDateTime(endDate);
        var exceptionEvents = await _context.StaffExceptions
            .Where(se => se.TenantId == tenantId.Value && !se.IsDeleted &&
                se.Date >= startOnly && se.Date <= endOnly)
            .Join(_context.StaffMembers, se => se.StaffId, s => s.Id, (se, s) => new { se, s })
            .Select(x => new
            {
                x.se.Id,
                title = $"{x.se.Type} - {x.s.FirstName} {x.s.LastName}",
                staffId = (Guid?)x.se.StaffId,
                staffName = $"{x.s.FirstName} {x.s.LastName}",
                start = x.se.Date.ToDateTime(TimeOnly.FromTimeSpan(x.se.StartTime ?? TimeSpan.Zero)).ToString("o"),
                end = x.se.Date.ToDateTime(TimeOnly.FromTimeSpan(x.se.EndTime ?? new TimeSpan(23, 59, 59))).ToString("o"),
                color = "#EF4444",
                type = "time_off"
            })
            .ToListAsync();

        var events = bookingEvents.Cast<object>().Concat(exceptionEvents.Cast<object>()).ToList();

        return Ok(new { data = events });
    }
}

public class ScheduleExceptionRequest
{
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = "time_off";
    public bool IsAllDay { get; set; } = true;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Reason { get; set; }
}

public class HoldSlotRequest
{
    public Guid ServiceId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime SlotDateTime { get; set; }
    public int DurationMinutes { get; set; }
}

public class WorkingHoursRequest
{
    public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday
    public bool IsWorkingDay { get; set; }
    public string? StartTime { get; set; } // e.g. "09:00"
    public string? EndTime { get; set; } // e.g. "17:00"
    public string? BreakStartTime { get; set; }
    public string? BreakEndTime { get; set; }
}

