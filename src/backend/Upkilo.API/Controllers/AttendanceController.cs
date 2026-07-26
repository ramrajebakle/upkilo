using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public AttendanceController(
            IAttendanceService attendanceService,
            ITenantProvider tenantProvider,
            AppDbContext context)
        {
            _attendanceService = attendanceService;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Staff clock-in
        /// </summary>
        [HttpPost("clock-in")]
        public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request)
        {
            var userId = _tenantProvider.GetUserId();
            var staff = await _context.Set<StaffMember>()
                .FirstOrDefaultAsync(s => s.TenantId == GetTenantId() && s.UserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _attendanceService.ClockInAsync(GetTenantId(), staff.Id, ip, request.LatLong, request.Device);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Staff clock-out
        /// </summary>
        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut()
        {
            var userId = _tenantProvider.GetUserId();
            var staff = await _context.Set<StaffMember>()
                .FirstOrDefaultAsync(s => s.TenantId == GetTenantId() && s.UserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            try
            {
                var result = await _attendanceService.ClockOutAsync(staff.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get my timesheet for the given period
        /// </summary>
        [HttpGet("my-timesheet")]
        public async Task<IActionResult> GetMyTimesheet([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = _tenantProvider.GetUserId();
            var staff = await _context.Set<StaffMember>()
                .FirstOrDefaultAsync(s => s.TenantId == GetTenantId() && s.UserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            var timesheet = await _attendanceService.GetStaffTimesheetAsync(staff.Id, start, end);
            return Ok(timesheet);
        }

        /// <summary>
        /// [Owner Only] Get attendance stats for the entire business
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetStats([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var stats = await _attendanceService.GetAttendanceStatsAsync(GetTenantId(), start, end);
            return Ok(stats);
        }
    }

    public class ClockInRequest
    {
        public string? LatLong { get; set; }
        public string? Device { get; set; }
    }
}
