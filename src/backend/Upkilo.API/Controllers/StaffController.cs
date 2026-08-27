using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Upkilo.API.Controllers;

/// <summary>
/// Staff management controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly ILogger<StaffController> _logger;
    private readonly AppDbContext _context;
    private readonly ISchedulingService _schedulingService;
    private readonly ITenantProvider _tenantProvider;

    private readonly IEventService _eventService;
    private readonly IMemoryCache _cache;

    public StaffController(ILogger<StaffController> logger, AppDbContext context, ISchedulingService schedulingService, ITenantProvider tenantProvider, IMemoryCache cache, IEventService eventService)
    {
        _logger = logger;
        _context = context;
        _schedulingService = schedulingService;
        _tenantProvider = tenantProvider;
        _cache = cache;
        _eventService = eventService;
    }

    /// <summary>
    /// Get all staff members
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStaff()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        string cacheKey = $"staff_list_{tenantId}";
        if (!_cache.TryGetValue(cacheKey, out List<object>? staff))
        {
            // The staff list page renders join date, specialties and booking counts.
            // None of them were projected, so the client read undefined for each and
            // displayed a confident zero: every member showed "offline", 0 bookings
            // today, 0 lifetime bookings and no specialties, regardless of the truth.
            // DateJoined and Tags are columns on the entity; the two counts are
            // subqueries EF translates in-database rather than loading bookings.
            var todayStart = DateTime.UtcNow.Date;
            var tomorrowStart = todayStart.AddDays(1);

            var staffEntries = await _context.StaffMembers
                .Where(s => s.TenantId == tenantId && !s.IsDeleted)
                .Select(s => new
                {
                    s.Id,
                    s.FirstName,
                    s.LastName,
                    s.Email,
                    s.Phone,
                    s.Role,
                    s.Color,
                    s.IsActive,
                    s.Title,
                    s.AvatarUrl,
                    s.DateJoined,
                    Specialties = s.Tags,
                    BookingsToday = s.Bookings.Count(b =>
                        b.StartTime >= todayStart &&
                        b.StartTime < tomorrowStart &&
                        b.Status != BookingStatus.Cancelled),
                    BookingsTotal = s.Bookings.Count(b => b.Status != BookingStatus.Cancelled)
                })
                .ToListAsync();

            staff = staffEntries.Cast<object>().ToList();
            _cache.Set(cacheKey, staff, TimeSpan.FromMinutes(10));
        }

        return Ok(new { data = staff });
    }

    /// <summary>
    /// Get staff member by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStaffMember(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var staff = await _context.StaffMembers
            .Include(s => s.StaffServices)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

        if (staff == null) return NotFound();

        return Ok(staff);
    }

    /// <summary>
    /// Get staff availability
    /// </summary>
    [HttpGet("{id}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(Guid id, [FromQuery] DateTime date, [FromQuery] Guid serviceId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var slots = await _schedulingService.GetAvailableSlotsAsync(tenantId.Value, serviceId, id, date);

        return Ok(new
        {
            staffId = id,
            date = date.Date,
            slots = slots.Select(s => new { time = s.ToString("HH:mm"), available = true })
        });
    }

    /// <summary>
    /// Create staff member
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var staff = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            Color = request.Color ?? "#3B82F6",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StaffMembers.Add(staff);

        if (request.ServiceIds != null && request.ServiceIds.Any())
        {
            foreach (var serviceId in request.ServiceIds)
            {
                _context.Set<StaffService>().Add(new StaffService
                {
                    Id = Guid.NewGuid(),
                    StaffId = staff.Id,
                    ServiceId = serviceId
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Staff member created: {StaffId}", staff.Id);

        return CreatedAtAction(nameof(GetStaffMember), new { id = staff.Id }, staff);
    }

    /// <summary>
    /// Update staff member
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStaffMember(Guid id, [FromBody] UpdateStaffRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var staff = await _context.StaffMembers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (staff == null) return NotFound();

        if (request.FirstName != null) staff.FirstName = request.FirstName;
        if (request.LastName != null) staff.LastName = request.LastName;
        if (request.Email != null) staff.Email = request.Email;
        if (request.Phone != null) staff.Phone = request.Phone;
        if (request.Role != null) staff.Role = request.Role;
        if (request.Color != null) staff.Color = request.Color;
        if (request.IsActive.HasValue) staff.IsActive = request.IsActive.Value;
        if (request.Bio != null) staff.Bio = request.Bio;
        if (request.Timezone != null) staff.Timezone = request.Timezone;
        if (request.HourlyRate.HasValue) staff.HourlyRate = request.HourlyRate.Value;
        if (request.BaseCommissionRate.HasValue) staff.BaseCommissionRate = request.BaseCommissionRate.Value;
        if (request.EmploymentType.HasValue) staff.EmploymentType = request.EmploymentType.Value;
        if (request.Tags != null) staff.Tags = request.Tags;
        if (request.CommissionType.HasValue) staff.CommissionType = request.CommissionType.Value;

        if (request.ServiceIds != null)
        {
            var existingServices = await _context.Set<StaffService>().Where(ss => ss.StaffId == staff.Id).ToListAsync();
            _context.Set<StaffService>().RemoveRange(existingServices);

            foreach (var serviceId in request.ServiceIds)
            {
                _context.Set<StaffService>().Add(new StaffService
                {
                    Id = Guid.NewGuid(),
                    StaffId = staff.Id,
                    ServiceId = serviceId
                });
            }
        }

        await _context.SaveChangesAsync();
        await _schedulingService.InvalidateStaffCacheAsync(tenantId.Value, id);
        _logger.LogInformation("Staff member updated: {StaffId}", id);

        return Ok(staff);
    }

    /// <summary>
    /// Update staff working hours
    /// </summary>
    [HttpPut("{id}/schedule")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] List<WorkingHoursDto> schedule)
    {
        // Remove existing
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify staff belongs to tenant
        var staff = await _context.StaffMembers.AnyAsync(s => s.Id == id && s.TenantId == tenantId);
        if (!staff) return NotFound();

        var existing = await _context.StaffWorkingHours.Where(wh => wh.StaffId == id).ToListAsync();
        _context.StaffWorkingHours.RemoveRange(existing);

        // Add new
        foreach (var item in schedule)
        {
            _context.StaffWorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                StaffId = id,
                DayOfWeek = item.DayOfWeek,
                IsWorkingDay = item.IsWorkingDay,
                StartTime = TimeSpan.Parse(item.Start),
                EndTime = TimeSpan.Parse(item.End),
                BreakStartTime = !string.IsNullOrEmpty(item.BreakStart) ? TimeSpan.Parse(item.BreakStart) : null,
                BreakEndTime = !string.IsNullOrEmpty(item.BreakEnd) ? TimeSpan.Parse(item.BreakEnd) : null,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await _schedulingService.InvalidateStaffCacheAsync(tenantId.Value, id);

        await _eventService.PublishAsync("staff.schedule_updated", new { StaffId = id, TenantId = tenantId.Value }, tenantId.Value);

        _logger.LogInformation("Staff schedule updated: {StaffId}", id);

        return Ok();
    }

    /// <summary>
    /// Delete staff member
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStaff(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var staff = await _context.StaffMembers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (staff == null || staff.IsDeleted) return NotFound();

        staff.IsDeleted = true;
        staff.DeletedAt = DateTime.UtcNow;
        staff.DeletedBy = User.FindFirst("id")?.Value;

        await _context.SaveChangesAsync();
        await _schedulingService.InvalidateStaffCacheAsync(tenantId.Value, id);

        _logger.LogInformation("Staff member softly deleted: {StaffId}", id);
        return NoContent();
    }

    /// <summary>
    /// Get staff shifts
    /// </summary>
    [HttpGet("{id}/shifts")]
    public async Task<IActionResult> GetShifts(Guid id) // Corrected method signature
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Ensure the staff member belongs to the tenant
        var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
        if (staffMember == null) return NotFound("Staff member not found or does not belong to your tenant.");

        var shifts = await _context.StaffShifts
            .Where(s => s.StaffId == id)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        return Ok(new { data = shifts });
    }

    /// <summary>
    /// Schedule staff shift
    /// </summary>
    [HttpPost("{id}/shifts")]
    public async Task<IActionResult> CreateShift(Guid id, [FromBody] CreateShiftRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify staff belongs to tenant
        var staffExists = await _context.StaffMembers.AnyAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted);
        if (!staffExists) return NotFound("Staff member not found.");

        // Verify location belongs to tenant
        var locationExists = await _context.Locations.AnyAsync(l => l.Id == request.LocationId && l.TenantId == tenantId);
        if (!locationExists) return BadRequest("Invalid location.");

        var shift = new StaffShift
        {
            Id = Guid.NewGuid(),
            StaffId = id,
            LocationId = request.LocationId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = ShiftStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        _context.StaffShifts.Add(shift);
        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("staff.shift_updated", new { StaffId = id, ShiftId = shift.Id, TenantId = tenantId.Value }, tenantId.Value);

        return Ok(shift);
    }

    /// <summary>
    /// Clock in
    /// </summary>
    [HttpPost("{id}/clock-in")]
    public async Task<IActionResult> ClockIn(Guid id, [FromBody] StaffClockInRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify staff belongs to tenant
        var staffExists = await _context.StaffMembers.AnyAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted);
        if (!staffExists) return NotFound("Staff member not found.");

        // Verify shift belongs to tenant if provided
        if (request.ShiftId.HasValue)
        {
            var shiftExists = await _context.StaffShifts.AnyAsync(s => s.Id == request.ShiftId && s.StaffId == id);
            if (!shiftExists) return BadRequest("Invalid shift.");
        }

        var active = await _context.StaffClockIns
            .FirstOrDefaultAsync(c => c.StaffId == id && c.ClockOutTime == null);

        if (active != null) return BadRequest("Already clocked in.");

        var clockIn = new StaffClockIn
        {
            Id = Guid.NewGuid(),
            StaffId = id,
            ShiftId = request.ShiftId,
            ClockInTime = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.StaffClockIns.Add(clockIn);
        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("staff.clock_in_out", new { StaffId = id, Action = "ClockIn", TenantId = tenantId.Value }, tenantId.Value);

        return Ok(clockIn);
    }

    /// <summary>
    /// Clock out
    /// </summary>
    [HttpPost("{id}/clock-out")]
    public async Task<IActionResult> ClockOut(Guid id)
    {
        var active = await _context.StaffClockIns
            .OrderByDescending(c => c.ClockInTime)
            .FirstOrDefaultAsync(c => c.StaffId == id && c.ClockOutTime == null);

        if (active == null) return NotFound("No active clock-in found.");

        active.ClockOutTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        await _eventService.PublishAsync("staff.clock_in_out", new { StaffId = id, Action = "ClockOut", TenantId = tenantId }, tenantId);

        return Ok(active);
    }

    /// <summary>
    /// Get staff commissions
    /// </summary>
    [HttpGet("{id}/commissions")]
    public async Task<IActionResult> GetCommissions(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify staff belongs to tenant
        var staffExists = await _context.StaffMembers.AnyAsync(s => s.Id == id && s.TenantId == tenantId);
        if (!staffExists) return NotFound("Staff member not found.");

        var commissions = await _context.StaffCommissions
            .Where(c => c.StaffId == id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(new { data = commissions });
    }

    /// <summary>
    /// Bulk update staff status
    /// </summary>
    [HttpPost("bulk-status")]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.StaffIds == null || !request.StaffIds.Any())
            return BadRequest("No staff IDs provided.");

        var staffToUpdate = await _context.StaffMembers
            .Where(s => request.StaffIds.Contains(s.Id) && s.TenantId == tenantId && !s.IsDeleted)
            .ToListAsync();

        foreach (var staff in staffToUpdate)
        {
            staff.IsActive = request.IsActive;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk updated status for {Count} staff members in tenant {TenantId}", staffToUpdate.Count, tenantId);

        return Ok(new { updatedCount = staffToUpdate.Count });
    }

    /// <summary>
    /// Get staff performance statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStaffStats([FromQuery] int days = 30)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days);

        var stats = await _context.StaffMembers
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                BookingCount = _context.Bookings.Count(b => b.StaffId == s.Id && b.StartTime >= startDate && b.Status == BookingStatus.Completed),
                Revenue = _context.Bookings.Where(b => b.StaffId == s.Id && b.StartTime >= startDate && b.Status == BookingStatus.Completed).Sum(b => b.Price ?? 0),
                ClockedInHours = _context.StaffClockIns
                    .Where(c => c.StaffId == s.Id && c.ClockInTime >= startDate && c.ClockOutTime != null)
                    .AsEnumerable()
                    .Sum(c => (c.ClockOutTime!.Value - c.ClockInTime).TotalHours)
            })
            .ToListAsync();

        return Ok(new { data = stats, periodDays = days });
    }

    /// <summary>
    /// Bulk delete staff members
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<Guid> staffIds)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var staffToUpdate = await _context.StaffMembers
            .Where(s => staffIds.Contains(s.Id) && s.TenantId == tenantId && !s.IsDeleted)
            .ToListAsync();

        foreach (var staff in staffToUpdate)
        {
            staff.IsDeleted = true;
            staff.DeletedAt = DateTime.UtcNow;
            staff.DeletedBy = User.FindFirst("id")?.Value;
        }

        await _context.SaveChangesAsync();
        _cache.Remove($"staff_list_{tenantId}");

        _logger.LogInformation("Bulk deleted {Count} staff members in tenant {TenantId}", staffToUpdate.Count, tenantId);

        return Ok(new { deletedCount = staffToUpdate.Count });
    }

    /// <summary>
    /// Get staff performance ranking (Leaderboard)
    /// </summary>
    [HttpGet("ranking")]
    public async Task<IActionResult> GetStaffRanking([FromQuery] int days = 30)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days);

        var ranking = await _context.StaffMembers
            .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                s.AvatarUrl,
                BookingCount = _context.Bookings.Count(b => b.StaffId == s.Id && b.StartTime >= startDate && b.Status == BookingStatus.Completed),
                Revenue = _context.Bookings.Where(b => b.StaffId == s.Id && b.StartTime >= startDate && b.Status == BookingStatus.Completed).Sum(b => b.Price ?? 0),
                Tips = _context.StaffCommissions.Where(c => c.StaffId == s.Id && c.CreatedAt >= startDate && c.Status == CommissionStatus.Approved).Sum(c => c.TipAmount)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        return Ok(new { startDate, periodDays = days, data = ranking });
    }

    /// <summary>
    /// Get staff utilization metrics (efficiency)
    /// </summary>
    [HttpGet("utilization")]
    public async Task<IActionResult> GetUtilization([FromQuery] int days = 30)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var startDate = DateTime.UtcNow.AddDays(-days);

        var metrics = await _context.StaffMembers
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                TotalClockedMinutes = _context.StaffClockIns
                    .Where(c => c.StaffId == s.Id && c.ClockInTime >= startDate && c.ClockOutTime != null)
                    .Select(c => (c.ClockOutTime!.Value - c.ClockInTime).TotalMinutes)
                    .Sum(),
                TotalBookingMinutes = _context.Bookings
                    .Where(b => b.StaffId == s.Id && b.StartTime >= startDate && b.Status == BookingStatus.Completed)
                    .Select(b => (b.EndTime - b.StartTime).TotalMinutes)
                    .Sum(),
            })
            .ToListAsync();

        var result = metrics.Select(m => new
        {
            m.Id,
            m.FirstName,
            m.LastName,
            TotalClockedMinutes = Math.Round(m.TotalClockedMinutes, 2),
            TotalBookingMinutes = Math.Round(m.TotalBookingMinutes, 2),
            UtilizationPercentage = m.TotalClockedMinutes > 0
                ? Math.Round((m.TotalBookingMinutes / m.TotalClockedMinutes) * 100, 2)
                : 0
        });

        return Ok(new { data = result, periodDays = days });
    }

    /// <summary>
    /// Request a shift swap
    /// </summary>
    [HttpPost("shifts/swap-request")]
    public async Task<IActionResult> RequestSwap([FromBody] RequestSwapRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var shift = await _context.StaffShifts
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && s.TenantId == tenantId);

        if (shift == null) return NotFound("Shift not found");

        var swapRequest = new StaffShiftSwap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            RequestingStaffId = shift.StaffId,
            RequestingShiftId = shift.Id,
            TargetStaffId = request.TargetStaffId,
            TargetShiftId = request.TargetShiftId,
            Reason = request.Reason,
            Status = SwapStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.StaffShiftSwaps.Add(swapRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shift swap requested by {StaffId} for shift {ShiftId}", shift.StaffId, request.ShiftId);

        return Ok(new { success = true, swapRequestId = swapRequest.Id });
    }

    /// <summary>
    /// Accept a shift swap request
    /// </summary>
    [HttpPost("shifts/swap-accept/{id}")]
    public async Task<IActionResult> AcceptSwap(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var swap = await _context.StaffShiftSwaps.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
        if (swap == null) return NotFound();

        swap.Status = SwapStatus.Accepted;
        swap.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Swap request accepted, pending admin approval." });
    }

    /// <summary>
    /// Approve a shift swap (Admin only)
    /// </summary>
    [HttpPost("shifts/swap-approve/{id}")]
    public async Task<IActionResult> ApproveSwap(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var swap = await _context.StaffShiftSwaps
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (swap == null) return NotFound();
        if (swap.Status != SwapStatus.Accepted) return BadRequest("Only accepted swaps can be approved");

        var reqShift = await _context.StaffShifts.FindAsync(swap.RequestingShiftId);

        if (swap.TargetShiftId.HasValue && swap.TargetStaffId.HasValue)
        {
            var tarShift = await _context.StaffShifts.FindAsync(swap.TargetShiftId.Value);

            if (reqShift != null && tarShift != null)
            {
                var tempStaffId = reqShift.StaffId;
                reqShift.StaffId = tarShift.StaffId;
                tarShift.StaffId = tempStaffId;
            }
        }
        else if (swap.TargetStaffId.HasValue)
        {
            if (reqShift != null)
            {
                reqShift.StaffId = swap.TargetStaffId.Value;
            }
        }

        swap.Status = SwapStatus.Approved;
        swap.ActionedAt = DateTime.UtcNow;
        swap.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("staff.shift_updated", new { StaffId = swap.TargetStaffId ?? swap.RequestingStaffId, TenantId = tenantId.Value }, tenantId.Value);

        _logger.LogInformation("Shift swap {SwapId} approved and finalized", id);

        return Ok(new { success = true });
    }
}

public record BulkUpdateStatusRequest(List<Guid> StaffIds, bool IsActive);
public record CreateShiftRequest(Guid LocationId, DateTime StartTime, DateTime EndTime);
public record StaffClockInRequest(Guid? ShiftId, string? LatLong);


public record RequestSwapRequest(
    Guid ShiftId,
    Guid? TargetStaffId = null,
    Guid? TargetShiftId = null,
    string? Reason = null
);

public record CreateStaffRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string? Color,
    List<Guid>? ServiceIds
);

public record UpdateStaffRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Role,
    string? Color,
    bool? IsActive,
    List<Guid>? ServiceIds,
    string? Bio = null,
    string? Timezone = null,
    decimal? HourlyRate = null,
    decimal? BaseCommissionRate = null,
    EmploymentType? EmploymentType = null,
    CommissionType? CommissionType = null,
    List<string>? Tags = null
);

public record WorkingHoursDto(
    int DayOfWeek,
    bool IsWorkingDay,
    string Start,
    string End,
    string? BreakStart,
    string? BreakEnd
);
