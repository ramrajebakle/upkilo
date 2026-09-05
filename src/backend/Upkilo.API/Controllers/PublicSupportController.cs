using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

/// <summary>
/// Anonymous Upkilo support chat for the marketing site.
///
/// Distinct from both authenticated assistants on purpose:
///   - AIChatbotController answers as a tenant's own assistant and needs a signed-in user.
///   - PublicReceptionistController answers as one business, identified by slug.
///   - This one answers as Upkilo itself, for a visitor who has no account and no business.
///
/// Because there is no tenant anywhere in this flow, there is no tenant data it can reach; see
/// <see cref="IPlatformSupportService"/>. What this controller owns is everything else an
/// internet-facing anonymous endpoint needs: rate limiting, unforgeable sessions, a bounded
/// transcript and a turn cap.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support")]
[AllowAnonymous]
public class PublicSupportController : ControllerBase
{
    private readonly IPlatformSupportService _support;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PublicSupportController> _logger;

    /// <summary>Turns replayed into the prompt, so "what about the cheaper one?" resolves.</summary>
    private const int HistoryTurns = 8;

    /// <summary>
    /// Hard ceiling on one session. The per-IP rate limit caps burst; this caps a slow drip from a
    /// single session that stays under it. Past this the visitor is pointed at a human.
    /// </summary>
    private const int MaxTurnsPerSession = 30;

    private static readonly DistributedCacheEntryOptions SessionOpts = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    public PublicSupportController(
        IPlatformSupportService support,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<PublicSupportController> logger)
    {
        _support = support;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/v1/support/chat — one turn of anonymous Upkilo support chat.
    /// </summary>
    [HttpPost("chat")]
    [EnableRateLimiting("support")]
    public async Task<IActionResult> Chat([FromBody] SupportChatRequest request)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
            return BadRequest(new { error = "Message must be 1-1000 characters." });

        // Session comes from a token this server signed, never from a caller-chosen id. A visitor
        // who could name their own session id could resume someone else's and read that
        // transcript back out of the prompt.
        var sessionId = ResolveSession(request.SessionToken) ?? Guid.NewGuid();
        var sessionToken = IssueSessionToken(sessionId);

        var key = $"support:session:{sessionId}";
        var cached = await _cache.GetStringAsync(key);
        var session = cached is null
            ? new SupportSession()
            : JsonSerializer.Deserialize<SupportSession>(cached) ?? new SupportSession();

        if (session.TurnCount >= MaxTurnsPerSession)
        {
            return Ok(new
            {
                reply = "We've covered a lot here. For anything further, email support@upkilo.com "
                        + "and the team will pick it up.",
                sessionToken,
                isFallback = true
            });
        }

        var history = string.Join("\n", session.Turns);

        var result = await _support.AskAsync(message, history, HttpContext.RequestAborted);

        session.TurnCount++;

        // Only a real exchange is remembered. Storing a rejected message would replay the
        // injection attempt into every later prompt in the session, and storing a generic
        // "I'm having trouble" line as context teaches the model to repeat it.
        if (!result.IsFallback)
        {
            session.Turns.Add($"Visitor: {message}");
            session.Turns.Add($"Assistant: {result.Reply}");

            // Keep the tail only, so prompt size stays flat however long the session runs.
            if (session.Turns.Count > HistoryTurns * 2)
                session.Turns.RemoveRange(0, session.Turns.Count - HistoryTurns * 2);
        }

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(session), SessionOpts);

        _logger.LogInformation(
            "[PlatformSupport] session={Session} turn={Turn} fallback={Fallback} rejected={Rejected}",
            sessionId, session.TurnCount, result.IsFallback, result.Rejected);

        return Ok(new
        {
            reply = result.Reply,
            sessionToken,
            isFallback = result.IsFallback
        });
    }

    private Guid? ResolveSession(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var sessionId)) return null;

        var provided = Encoding.UTF8.GetBytes(parts[1]);
        var expected = Encoding.UTF8.GetBytes(Sign(sessionId));

        // Fixed-time comparison; a short-circuiting compare leaks the signature byte by byte to
        // anyone willing to make enough requests.
        if (provided.Length != expected.Length) return null;
        return CryptographicOperations.FixedTimeEquals(provided, expected) ? sessionId : null;
    }

    private string IssueSessionToken(Guid sessionId) => $"{sessionId}.{Sign(sessionId)}";

    /// <summary>
    /// The purpose string is part of the signed payload, so a support token cannot be replayed as
    /// a receptionist session token even though both are signed with the same secret.
    /// </summary>
    private string Sign(Guid sessionId)
    {
        var secret = _configuration["Support:SessionSecret"]
                     ?? _configuration["Jwt:Secret"]
                     ?? throw new InvalidOperationException(
                         "No signing secret configured for support sessions (Jwt:Secret).");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var payload = Encoding.UTF8.GetBytes($"platform-support-session|{sessionId}");
        return Convert.ToBase64String(hmac.ComputeHash(payload))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed class SupportSession
    {
        public int TurnCount { get; set; }
        public List<string> Turns { get; set; } = new();
    }
}

/// <summary>
/// SessionToken is opaque and server-issued: omit it on the first turn, then echo back whatever
/// the previous response returned.
/// </summary>
public record SupportChatRequest(string? Message, string? SessionToken);
