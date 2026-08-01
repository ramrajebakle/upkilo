using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Staff,Admin,Owner")]
public class StaffTimesheetsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public StaffTimesheetsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetTimesheets([FromQuery] Guid? staffId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.StaffTimesheets.Where(t => t.TenantId == tenantId);

        // Security: If not Admin/Owner, can only view their own
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);

            if (staffMember == null) return Forbid();
            query = query.Where(t => t.StaffId == staffMember.Id);
        }
        else if (staffId.HasValue)
        {
            query = query.Where(t => t.StaffId == staffId.Value);
        }

        if (startDate.HasValue) query = query.Where(t => t.ClockInTime >= startDate.Value);
        if (endDate.HasValue) query = query.Where(t => t.ClockInTime <= endDate.Value);

        var timesheets = await query.OrderByDescending(t => t.ClockInTime).ToListAsync();
        return Ok(timesheets);
    }

    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
        if (staffMember == null) return Unauthorized("Linked staff member not found.");

        // Check if already clocked in
        var activeTimesheet = await _context.StaffTimesheets
            .FirstOrDefaultAsync(t => t.StaffId == staffMember.Id && t.ClockOutTime == null);

        if (activeTimesheet != null)
            return BadRequest(new { message = "Already clocked in.", clockInTime = activeTimesheet.ClockInTime });

        var timesheet = new StaffTimesheet
        {
            TenantId = tenantId.Value,
            StaffId = staffMember.Id,
            ClockInTime = DateTime.UtcNow,
            Status = "Pending"
        };

        _context.StaffTimesheets.Add(timesheet);
        await _context.SaveChangesAsync();

        return Ok(timesheet);
    }

    [HttpPost("clock-out")]
    public async Task<IActionResult> ClockOut()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
        if (staffMember == null) return Unauthorized("Linked staff member not found.");

        var activeTimesheet = await _context.StaffTimesheets
            .FirstOrDefaultAsync(t => t.StaffId == staffMember.Id && t.ClockOutTime == null);

        if (activeTimesheet == null)
            return BadRequest(new { message = "No active clock-in session found." });

        activeTimesheet.ClockOutTime = DateTime.UtcNow;
        activeTimesheet.TotalHours = (decimal?)(activeTimesheet.ClockOutTime.Value - activeTimesheet.ClockInTime).TotalHours;
        activeTimesheet.Status = "Completed";

        await _context.SaveChangesAsync();

        return Ok(activeTimesheet);
    }
}
