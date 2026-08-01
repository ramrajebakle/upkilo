using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Class scheduling — group sessions, capacity management, recurring classes
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ClassSchedulingController : ControllerBase
{
    private readonly ILogger<ClassSchedulingController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ClassSchedulingController(ILogger<ClassSchedulingController> logger, AppDbContext context, ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>GET /api/v1/classscheduling — list group/class sessions</summary>
    [HttpGet]
    public async Task<IActionResult> GetClasses(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.GroupBookings
            .Include(g => g.Participants)
            .Where(g => g.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<GroupBookingStatus>(status, true, out var statusEnum))
            query = query.Where(g => g.Status == statusEnum);

        var classes = await query
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new
            {
                id = g.Id,
                groupName = g.GroupName ?? "Unnamed Class",
                maxParticipants = g.MaxParticipants,
                currentParticipants = g.CurrentParticipants,
                status = g.Status.ToString(),
                totalPrice = g.TotalPrice,
                isPublic = g.IsPublic,
                notes = g.Notes,
                createdAt = g.CreatedAt,
                spotsRemaining = g.MaxParticipants - g.CurrentParticipants,
                isFull = g.CurrentParticipants >= g.MaxParticipants,
                participantCount = g.Participants.Count,
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            classes,
            total = classes.Count,
            open = classes.Count(c => c.status == "Open"),
            full = classes.Count(c => c.isFull),
        }));
    }

    /// <summary>POST /api/v1/classscheduling — create new class/group session</summary>
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.GroupName)) return BadRequest(ApiResponse.Fail("Class name required"));
        if (request.MaxParticipants < 2) return BadRequest(ApiResponse.Fail("Class must have at least 2 spots"));

        // Use provided service parameters or generate an underlying Booking later
        Guid masterBookingId = Guid.NewGuid(); // To be associated with a scheduled Booking Entity in future iterations

        var groupBooking = new GroupBooking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            MasterBookingId = masterBookingId,
            OrganizerId = tenantId.Value, // Business owner as organizer
            GroupName = request.GroupName,
            MaxParticipants = request.MaxParticipants,
            CurrentParticipants = 0,
            Status = GroupBookingStatus.Open,
            TotalPrice = request.PricePerParticipant * request.MaxParticipants,
            IsPublic = request.IsPublic,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        _context.GroupBookings.Add(groupBooking);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Class {Name} created for tenant {TenantId}", request.GroupName, tenantId);
        return Ok(ApiResponse<object>.Ok(new
        {
            id = groupBooking.Id,
            groupName = groupBooking.GroupName,
            maxParticipants = groupBooking.MaxParticipants,
            status = groupBooking.Status.ToString(),
        }));
    }

    /// <summary>POST /api/v1/classscheduling/{id}/enroll — enroll a client</summary>
    [HttpPost("{id:guid}/enroll")]
    public async Task<IActionResult> EnrollClient(Guid id, [FromBody] EnrollClientRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var groupBooking = await _context.GroupBookings
            .Include(g => g.Participants)
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId);

        if (groupBooking == null) return NotFound(ApiResponse.Fail("Class not found"));
        if (groupBooking.Status != GroupBookingStatus.Open) return BadRequest(ApiResponse.Fail("Class is not accepting enrollments"));
        if (groupBooking.CurrentParticipants >= groupBooking.MaxParticipants)
            return BadRequest(ApiResponse.Fail("Class is full"));

        // Check if already enrolled
        if (request.ClientId.HasValue && groupBooking.Participants.Any(p => p.ClientId == request.ClientId))
            return BadRequest(ApiResponse.Fail("Client already enrolled"));

        var participant = new GroupBookingParticipant
        {
            Id = Guid.NewGuid(),
            GroupBookingId = id,
            ClientId = request.ClientId,
            GuestName = request.GuestName,
            GuestEmail = request.GuestEmail,
            GuestPhone = request.GuestPhone,
            Status = ParticipantStatus.Confirmed,
            IndividualPrice = groupBooking.TotalPrice / groupBooking.MaxParticipants,
            CreatedAt = DateTime.UtcNow,
        };

        groupBooking.CurrentParticipants++;
        if (groupBooking.CurrentParticipants >= groupBooking.MaxParticipants)
            groupBooking.Status = GroupBookingStatus.Full;

        _context.GroupBookingParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            participantId = participant.Id,
            spotsRemaining = groupBooking.MaxParticipants - groupBooking.CurrentParticipants,
            isFull = groupBooking.CurrentParticipants >= groupBooking.MaxParticipants,
        }));
    }

    /// <summary>PUT /api/v1/classscheduling/{id}/status — update class status</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateClassStatusRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var groupBooking = await _context.GroupBookings
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId);

        if (groupBooking == null) return NotFound();

        if (Enum.TryParse<GroupBookingStatus>(request.Status, true, out var newStatus))
            groupBooking.Status = newStatus;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id, status = groupBooking.Status.ToString() }));
    }

    /// <summary>DELETE /api/v1/classscheduling/{id} — cancel a class</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelClass(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var groupBooking = await _context.GroupBookings
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId);

        if (groupBooking == null) return NotFound();

        groupBooking.Status = GroupBookingStatus.Cancelled;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    // ────────────────────────────────────────────────────────────
    // Recurring classes — GroupBookingRecurrence
    // ────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/classscheduling/{id}/recurrence — configure recurring schedule for a class</summary>
    [HttpPost("{id:guid}/recurrence")]
    public async Task<IActionResult> SetRecurrence(Guid id, [FromBody] SetRecurrenceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var groupBooking = await _context.GroupBookings
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId);

        if (groupBooking == null) return NotFound(ApiResponse.Fail("Class not found"));

        var schedule = await _context.GroupBookingRecurrences.FirstOrDefaultAsync(r => r.ClassId == id && r.TenantId == tenantId);

        if (schedule == null)
        {
            schedule = new GroupBookingRecurrence
            {
                Id = Guid.NewGuid(),
                ClassId = id,
                TenantId = tenantId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.GroupBookingRecurrences.Add(schedule);
        }

        schedule.Frequency = request.Frequency;
        schedule.DaysOfWeek = request.DaysOfWeek ?? Array.Empty<string>();
        schedule.StartDate = request.StartDate;
        schedule.EndDate = request.EndDate;
        schedule.StartTime = request.StartTime;
        schedule.DurationMinutes = request.DurationMinutes;
        schedule.MaxParticipants = request.MaxParticipants ?? groupBooking.MaxParticipants;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var instances = GenerateInstances(schedule, 8);
        _logger.LogInformation("Recurrence set for class {ClassId}: {Freq}", id, request.Frequency);

        return Ok(ApiResponse<object>.Ok(new { schedule, instanceCount = instances.Count, upcomingInstances = instances }));
    }

    /// <summary>GET /api/v1/classscheduling/{id}/recurrence — get recurring schedule</summary>
    [HttpGet("{id:guid}/recurrence")]
    public async Task<IActionResult> GetRecurrence(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var schedule = await _context.GroupBookingRecurrences.FirstOrDefaultAsync(r => r.ClassId == id && r.TenantId == tenantId);

        if (schedule == null)
            return NotFound(ApiResponse.Fail("No recurring schedule found"));

        var instances = GenerateInstances(schedule, 8);
        return Ok(ApiResponse<object>.Ok(new { schedule, upcomingInstances = instances }));
    }

    /// <summary>DELETE /api/v1/classscheduling/{id}/recurrence — remove recurring schedule</summary>
    [HttpDelete("{id:guid}/recurrence")]
    public async Task<IActionResult> DeleteRecurrence(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var schedule = await _context.GroupBookingRecurrences.FirstOrDefaultAsync(r => r.ClassId == id && r.TenantId == tenantId);
        if (schedule != null)
        {
            _context.GroupBookingRecurrences.Remove(schedule);
            await _context.SaveChangesAsync();
        }

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    private static List<object> GenerateInstances(GroupBookingRecurrence schedule, int count)
    {
        var instances = new List<object>();
        var current = schedule.StartDate;
        var end = schedule.EndDate ?? current.AddYears(1);
        var generated = 0;

        while (current <= end && generated < count)
        {
            bool include = schedule.Frequency switch
            {
                "daily" => true,
                "weekly" => schedule.DaysOfWeek.Length == 0 || schedule.DaysOfWeek.Contains(current.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase),
                "biweekly" => (schedule.DaysOfWeek.Length == 0 || schedule.DaysOfWeek.Contains(current.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase))
                             && (current - schedule.StartDate).TotalDays % 14 < 7,
                "monthly" => current.Day == schedule.StartDate.Day,
                _ => false,
            };

            if (include)
            {
                if (TimeSpan.TryParse(schedule.StartTime, out var parsedTime))
                {
                    var dt = current.Date.Add(parsedTime);
                    instances.Add(new
                    {
                        instanceDate = current.ToShortDateString(),
                        startTime = dt.ToString("o"),
                        endTime = dt.AddMinutes(schedule.DurationMinutes).ToString("o"),
                        maxParticipants = schedule.MaxParticipants,
                    });
                }
                generated++;
            }
            current = current.AddDays(1);
        }

        return instances;
    }
}

public class CreateClassRequest
{
    public string GroupName { get; set; } = string.Empty;
    public int MaxParticipants { get; set; } = 10;
    public decimal PricePerParticipant { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? Notes { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
}

public class EnrollClientRequest
{
    public Guid? ClientId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
}

public class UpdateClassStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SetRecurrenceRequest
{
    public string Frequency { get; set; } = string.Empty;
    public string[]? DaysOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int? MaxParticipants { get; set; }
}
