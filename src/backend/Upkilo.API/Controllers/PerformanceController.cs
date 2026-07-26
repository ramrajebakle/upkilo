using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Performance and analytics controller for staff and business
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public PerformanceController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get multi-staff performance metrics
    /// </summary>
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffPerformance([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var startDate = start ?? DateTime.UtcNow.AddDays(-30);
        var endDate = end ?? DateTime.UtcNow;

        var stats = await _context.StaffMembers
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                TotalBookings = s.Bookings.Count(b => b.StartTime >= startDate && b.StartTime <= endDate),
                Revenue = s.Bookings
                    .Where(b => b.StartTime >= startDate && b.StartTime <= endDate && b.Status == BookingStatus.Completed)
                    .Sum(b => b.Price),
                Commissions = _context.StaffCommissions
                    .Where(c => c.StaffId == s.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                    .Sum(c => c.TotalEarned),
                Tips = _context.StaffCommissions
                    .Where(c => c.StaffId == s.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                    .Sum(c => c.TipAmount)
            })
            .ToListAsync();

        return Ok(new { data = stats });
    }

    /// <summary>
    /// Get business-wide commission report
    /// </summary>
    [HttpGet("commissions")]
    public async Task<IActionResult> GetCommissionsReport([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var startDate = start ?? DateTime.UtcNow.AddDays(-30);
        var endDate = end ?? DateTime.UtcNow;

        var report = await _context.StaffCommissions
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .Include(c => c.Staff)
            .GroupBy(c => new { c.StaffId, c.Staff!.FirstName, c.Staff.LastName })
            .Select(g => new
            {
                StaffId = g.Key.StaffId,
                StaffName = $"{g.Key.FirstName} {g.Key.LastName}",
                TotalCommissions = g.Count(),
                TotalEarned = g.Sum(c => c.TotalEarned),
                TotalTips = g.Sum(c => c.TipAmount),
                StatusBreakdown = g.GroupBy(c => c.Status).Select(sg => new { Status = sg.Key, Count = sg.Count() })
            })
            .ToListAsync();

        return Ok(new { data = report });
    }
}

