using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/auth/sessions")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ISessionService sessionService, ILogger<SessionController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Get all active sessions for current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetUserId();
        var sessions = await _sessionService.GetActiveSessionsAsync(userId);

        // Extract current session ID from JWT 'sid' claim (set during login)
        var currentSessionId = User.FindFirst("sid")?.Value;
        Guid.TryParse(currentSessionId, out var currentSid);

        return Ok(sessions.Select(s => new
        {
            s.Id,
            s.DeviceType,
            s.Browser,
            s.OperatingSystem,
            s.IpAddress,
            s.Location,
            s.LastActiveAt,
            s.CreatedAt,
            isCurrent = s.Id == currentSid
        }));
    }

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        var success = await _sessionService.RevokeSessionAsync(id, GetUserId());
        if (!success)
            return NotFound(new { error = "Session not found" });

        return Ok(new { message = "Session has been revoked" });
    }

    /// <summary>
    /// Revoke all sessions except current
    /// </summary>
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAllSessions([FromBody] RevokeAllRequest? request)
    {
        var count = await _sessionService.RevokeAllSessionsAsync(GetUserId(), request?.ExceptSessionId);
        return Ok(new { message = $"{count} session(s) have been revoked" });
    }
}

public record RevokeAllRequest(Guid? ExceptSessionId);
