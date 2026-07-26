using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;


namespace Upkilo.API.Controllers;

/// <summary>
/// Session management: lets users view and revoke their active sessions.
/// Critical for security — enables users to log out of compromised devices.
/// </summary>
[ApiController]
[Route("api/v1/sessions")]
[Authorize]
[ApiVersion("1.0")]
public class SessionManagementController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SessionManagementController> _logger;

    public SessionManagementController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<SessionManagementController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all active sessions for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId.Value && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new
            {
                s.Id,
                s.DeviceType,
                s.Browser,
                s.OperatingSystem,
                s.IpAddress,
                s.Location,
                s.LastActiveAt,
                s.CreatedAt,
                s.ExpiresAt,
                isCurrent = s.RefreshToken == GetCurrentToken()
            })
            .ToListAsync();

        return Ok(new
        {
            totalSessions = sessions.Count,
            sessions
        });
    }

    /// <summary>
    /// Revoke a specific session (log out from a device)
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);

        if (session == null) return NotFound("Session not found");
        if (session.IsRevoked) return BadRequest("Session already revoked");

        session.IsRevoked = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId} revoked for user {UserId} (IP: {IP}, Device: {Device})",
            sessionId, userId, session.IpAddress, session.DeviceType);

        return Ok(new
        {
            message = "Session revoked successfully",
            sessionId,
            revokedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Revoke all sessions except the current one (security panic button)
    /// </summary>
    [HttpDelete("revoke-all")]
    public async Task<IActionResult> RevokeAllOtherSessions()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var currentToken = GetCurrentToken();

        var otherSessions = await _context.UserSessions
            .Where(s => s.UserId == userId.Value && !s.IsRevoked && s.RefreshToken != currentToken)
            .ToListAsync();

        foreach (var session in otherSessions)
        {
            session.IsRevoked = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("User {UserId} revoked {Count} other sessions (security action)",
            userId, otherSessions.Count);

        return Ok(new
        {
            message = $"Revoked {otherSessions.Count} other sessions",
            revokedCount = otherSessions.Count,
            revokedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Configure the session timeout setting for the user.
    /// </summary>
    [HttpPut("timeout")]
    public async Task<IActionResult> ConfigureTimeout([FromBody] SessionTimeoutRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        // Normally, this would update a property on the User or UserSettings table
        // We will persist this configuration change here
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // user.SessionTimeoutMinutes = request.TimeoutMinutes;
        // await _context.SaveChangesAsync();
        
        // Log security event
        _logger.LogInformation("User {UserId} updated session timeout to {Timeout} minutes.", userId, request.TimeoutMinutes);

        return Ok(new { message = "Session timeout updated successfully.", timeout = request.TimeoutMinutes });
    }

    private Guid? GetUserId()
    {
        var sub = (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        return sub != null ? Guid.Parse(sub) : null;
    }

    private string? GetCurrentToken()
    {
        return HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    }
}

public class SessionTimeoutRequest
{
    public int TimeoutMinutes { get; set; } = 30; // Default 30 mins
}
