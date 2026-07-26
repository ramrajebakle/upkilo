using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;


namespace Upkilo.API.Controllers;

/// <summary>
/// Login history and suspicious activity monitoring.
/// Lets users review login activity and detect unauthorized access.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/login-history")]
[Authorize]
public class LoginHistoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoginHistoryController> _logger;

    public LoginHistoryController(AppDbContext context, ILogger<LoginHistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get recent login history for the current user (last 90 days)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLoginHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool suspiciousOnly = false)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var query = _context.Set<LoginHistory>()
            .Where(h => h.UserId == userId.Value)
            .Where(h => h.AttemptedAt > DateTime.UtcNow.AddDays(-90));

        if (suspiciousOnly)
            query = query.Where(h => h.IsSuspicious);

        var total = await query.CountAsync();
        var history = await query
            .OrderByDescending(h => h.AttemptedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.IpAddress,
                h.Browser,
                h.OperatingSystem,
                h.DeviceType,
                h.Location,
                h.Result,
                h.FailureReason,
                h.AttemptedAt,
                h.IsSuspicious,
                h.SuspiciousReason
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            history
        });
    }

    /// <summary>
    /// Get login analytics summary for security dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetLoginSummary()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var logs = await _context.Set<LoginHistory>()
            .Where(h => h.UserId == userId.Value && h.AttemptedAt > thirtyDaysAgo)
            .ToListAsync();

        var uniqueDevices = logs
            .Select(h => $"{h.Browser}|{h.OperatingSystem}|{h.DeviceType}")
            .Distinct()
            .Count();

        var uniqueLocations = logs
            .Where(h => h.Location != null)
            .Select(h => h.Location)
            .Distinct()
            .Count();

        return Ok(new
        {
            last30Days = new
            {
                totalAttempts = logs.Count,
                successfulLogins = logs.Count(h => h.Result == LoginResult.Success),
                failedAttempts = logs.Count(h => h.Result != LoginResult.Success),
                suspiciousAttempts = logs.Count(h => h.IsSuspicious),
                uniqueDevices,
                uniqueLocations,
                uniqueIPs = logs.Select(h => h.IpAddress).Distinct().Count()
            },
            lastLogin = logs
                .Where(h => h.Result == LoginResult.Success)
                .OrderByDescending(h => h.AttemptedAt)
                .Select(h => new { h.AttemptedAt, h.IpAddress, h.Location, h.DeviceType })
                .FirstOrDefault(),
            lastFailedAttempt = logs
                .Where(h => h.Result != LoginResult.Success)
                .OrderByDescending(h => h.AttemptedAt)
                .Select(h => new { h.AttemptedAt, h.IpAddress, h.Result, h.FailureReason })
                .FirstOrDefault()
        });
    }

    private Guid? GetUserId()
    {
        var sub = (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        return sub != null ? Guid.Parse(sub) : null;
    }
}
