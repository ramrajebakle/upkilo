using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace Upkilo.API.Controllers;

/// <summary>
/// Issues single-use 30-second opaque tickets for SignalR hub connections.
/// Clients pass the ticket (not the raw JWT) in the access_token query string,
/// preventing JWT tokens from appearing in web-server access logs and browser history.
/// Flow: POST /api/v1/signalr/ticket → { ticket } → connect?access_token={ticket}
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/signalr")]
[Authorize]
public class SignalRTicketController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public SignalRTicketController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpPost("ticket")]
    public async Task<IActionResult> IssueTicket()
    {
        // Extract the raw JWT from the Authorization header or the httpOnly cookie
        var jwt = Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrEmpty(jwt) && Request.Cookies.TryGetValue("token", out var cookieJwt))
            jwt = cookieJwt;

        if (string.IsNullOrEmpty(jwt))
            return Unauthorized(new { message = "No JWT found to exchange for a ticket." });

        // Generate a cryptographically-random 32-byte ticket (64 hex chars)
        var ticketBytes = new byte[32];
        RandomNumberGenerator.Fill(ticketBytes);
        var ticket = Convert.ToHexString(ticketBytes).ToLowerInvariant();

        await _cache.SetStringAsync(
            $"signalr:ticket:{ticket}",
            jwt,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) });

        return Ok(new { ticket });
    }
}
