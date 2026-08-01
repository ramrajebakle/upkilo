using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Middleware;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Upkilo.API.Controllers;

/// <summary>
/// Campaign engagement tracking — open pixel, click redirect, reply webhook.
/// These endpoints are public (AllowAnonymous) because they are called directly
/// from email clients and mail servers, NOT from the authenticated frontend.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns/track")]
public class CampaignTrackingController : ControllerBase
{
    private readonly ILogger<CampaignTrackingController> _logger;
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConfiguration _configuration;

    // In-memory event buffer for request-scoped aggregation; events are also persisted to DB.
    internal static readonly ConcurrentDictionary<string, List<TrackingEvent>> _events = new();

    // Secret used to sign/verify tracking tokens (loaded from configuration).
    private readonly string _tokenSecret;

    // 1×1 transparent GIF bytes
    private static readonly byte[] _transparentGif = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    public CampaignTrackingController(
        ILogger<CampaignTrackingController> logger,
        AppDbContext db,
        ITenantProvider tenantProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _db = db;
        _tenantProvider = tenantProvider;
        _configuration = configuration;
        // VULN-A08 FIX: never fall back to a source-committed default; throw at startup instead.
        _tokenSecret = configuration["CampaignTracking:TokenSecret"]
            ?? throw new InvalidOperationException(
                "CampaignTracking:TokenSecret must be set in configuration. " +
                "Generate with: openssl rand -base64 32");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/campaigns/track/open?t={token}
    // Called when the email client renders the tracking pixel.
    // Returns a 1×1 transparent GIF and records the open event.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("open")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TrackOpen([FromQuery] string t)
    {
        if (!string.IsNullOrWhiteSpace(t))
        {
            var payload = DecodeToken(t);
            if (payload != null)
            {
                RecordEvent(payload.CampaignId, new TrackingEvent
                {
                    Type = "open",
                    RecipientId = payload.RecipientId,
                    CampaignId = payload.CampaignId,
                    OccurredAt = DateTime.UtcNow,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                _logger.LogDebug("Email open tracked: campaign={CampaignId} recipient={RecipientId}",
                    payload.CampaignId, payload.RecipientId);
                _ = PersistEventAsync(payload.CampaignId, "open");
            }
        }

        // Always return the pixel regardless of token validity
        return File(_transparentGif, "image/gif");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/campaigns/track/click?t={token}&url={destinationUrl}
    // Called when a tracked link in the email is clicked.
    // Records the click, then 302-redirects to the real URL.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("click")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TrackClick([FromQuery] string t, [FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("Missing destination url");

        // Basic URL safety check — must be http(s) to prevent redirect attacks
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Invalid destination url");

        // VULN-A07 FIX: restrict redirect target to known Upkilo domains only.
        // Bare http(s) prefix check allows phishing via trusted api.upkilo.com domain.
        if (!IsAllowedRedirectTarget(url))
            return BadRequest("Redirect target is not an allowed domain.");

        if (!string.IsNullOrWhiteSpace(t))
        {
            var payload = DecodeToken(t);
            if (payload != null)
            {
                RecordEvent(payload.CampaignId, new TrackingEvent
                {
                    Type = "click",
                    RecipientId = payload.RecipientId,
                    CampaignId = payload.CampaignId,
                    OccurredAt = DateTime.UtcNow,
                    Url = url,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                _logger.LogDebug("Link click tracked: campaign={CampaignId} url={Url}",
                    payload.CampaignId, url);
                _ = PersistEventAsync(payload.CampaignId, "click");
            }
        }

        return Redirect(url);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/campaigns/track/reply
    // Inbound reply webhook — called by ESP (SendGrid, Mailgun, etc.) when
    // a recipient replies to a campaign email.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("reply")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TrackReply([FromBody] ReplyWebhookPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.CampaignId))
            return Ok(); // Silently accept; don't expose errors to mail servers

        RecordEvent(payload.CampaignId, new TrackingEvent
        {
            Type = "reply",
            RecipientId = payload.RecipientEmail ?? "unknown",
            CampaignId = payload.CampaignId,
            OccurredAt = DateTime.UtcNow,
            ReplyText = payload.Text?.Length > 500 ? payload.Text[..500] : payload.Text
        });

        _logger.LogInformation("Campaign reply received: campaign={CampaignId} from={From}",
            payload.CampaignId, payload.RecipientEmail);

        return Ok(new { received = true });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/campaigns/track/unsubscribe
    // One-click unsubscribe link handler (RFC 8058)
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("unsubscribe")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackUnsubscribe([FromQuery] string t)
    {
        if (string.IsNullOrWhiteSpace(t))
            return BadRequest("Missing token");

        var payload = DecodeToken(t);
        if (payload == null)
            return BadRequest("Invalid token");

        RecordEvent(payload.CampaignId, new TrackingEvent
        {
            Type = "unsubscribe",
            RecipientId = payload.RecipientId,
            CampaignId = payload.CampaignId,
            OccurredAt = DateTime.UtcNow
        });

        // Attempt to mark the client as unsubscribed in the DB
        try
        {
            var client = await _db.Clients
                .FirstOrDefaultAsync(c => c.Email == payload.RecipientId);

            if (client != null)
            {
                client.MarketingConsent = false;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist unsubscribe for {Email}", payload.RecipientId);
        }

        _logger.LogInformation("Unsubscribe: campaign={CampaignId} recipient={RecipientId}",
            payload.CampaignId, payload.RecipientId);
        _ = PersistEventAsync(payload.CampaignId, "unsubscribe");

        return Ok(new { unsubscribed = true });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/campaigns/track/{campaignId}/events
    //      ?type=open|click|reply&page=1&limit=50
    // Authenticated — returns raw event log for a campaign (admin view)
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("{campaignId}/events")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetEvents(
        string campaignId,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        var all = _events.TryGetValue(campaignId, out var list)
            ? list.AsEnumerable()
            : Enumerable.Empty<TrackingEvent>();

        if (!string.IsNullOrWhiteSpace(type))
            all = all.Where(e => e.Type == type);

        var ordered = all.OrderByDescending(e => e.OccurredAt).ToList();
        var total = ordered.Count;
        var paged = ordered.Skip((page - 1) * limit).Take(limit).ToList();

        // Aggregate summary
        var opens = ordered.Count(e => e.Type == "open");
        var clicks = ordered.Count(e => e.Type == "click");
        var replies = ordered.Count(e => e.Type == "reply");
        var unsubs = ordered.Count(e => e.Type == "unsubscribe");
        var uniqueOpeners = ordered.Where(e => e.Type == "open").Select(e => e.RecipientId).Distinct().Count();
        var uniqueClickers = ordered.Where(e => e.Type == "click").Select(e => e.RecipientId).Distinct().Count();

        return Ok(ApiResponse<object>.Ok(new
        {
            summary = new
            {
                opens,
                clicks,
                replies,
                unsubscribes = unsubs,
                uniqueOpeners,
                uniqueClickers
            },
            events = paged,
            total,
            page,
            limit
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/campaigns/track/{campaignId}/timeline
    //      Returns event counts grouped by hour for the past 7 days
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("{campaignId}/timeline")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetTimeline(string campaignId, [FromQuery] int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var all = _events.TryGetValue(campaignId, out var list)
            ? list.Where(e => e.OccurredAt >= cutoff).ToList()
            : new List<TrackingEvent>();

        // Group by day
        var timeline = all
            .GroupBy(e => e.OccurredAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                opens = g.Count(e => e.Type == "open"),
                clicks = g.Count(e => e.Type == "click"),
                replies = g.Count(e => e.Type == "reply")
            })
            .ToList();

        return Ok(ApiResponse<object>.Ok(new { timeline, days }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    // VULN-A07: Allowlist — only redirect to known Upkilo domains.
    private static readonly HashSet<string> _allowedRedirectHosts = new(StringComparer.OrdinalIgnoreCase)
        { "upkilo.com", "app.upkilo.com", "www.upkilo.com", "booking.upkilo.com" };

    private static bool IsAllowedRedirectTarget(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != "https" && parsed.Scheme != "http") return false;
        return _allowedRedirectHosts.Contains(parsed.Host);
    }

    // VULN-A12: Bound the in-memory event store — evict oldest campaign when over limit.
    private const int MaxCampaignKeys = 500;
    private static void EvictIfNeeded()
    {
        if (_events.Count > MaxCampaignKeys)
        {
            var oldest = _events.Keys.Take(_events.Count - MaxCampaignKeys).ToList();
            foreach (var k in oldest) _events.TryRemove(k, out _);
        }
    }

    /// <summary>
    /// Generate an HMAC-SHA256-signed tracking token.
    /// Format: base64url(campaignId|recipientEmail|timestamp) + "." + base64url(HMAC)
    /// </summary>
    // [NonAction]: this is an internal helper, not an endpoint. Without it, MVC treats the public
    // instance method as an action with no HTTP verb, which throws SwaggerGeneratorException
    // ("Ambiguous HTTP method") and makes /swagger/v1/swagger.json return 500.
    [NonAction]
    public string GenerateToken(string campaignId, string recipientEmail)
    {
        var payload = $"{campaignId}|{recipientEmail}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_tokenSecret));
        var sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{payloadB64}.{sig}";
    }

    // Allow the static GenerateToken to be called from other controllers (now instance method above).
    // This static overload accepts an explicit secret for backward compatibility.
    public static string GenerateToken(string campaignId, string recipientEmail, string secret)
    {
        var payload = $"{campaignId}|{recipientEmail}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{payloadB64}.{sig}";
    }

    private TrackingTokenPayload? DecodeToken(string token)
    {
        try
        {
            var parts = token.Split('.', 2);
            if (parts.Length != 2) return null;

            // Verify HMAC signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_tokenSecret));
            var expectedSig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0])))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSig),
                    Encoding.UTF8.GetBytes(parts[1])))
                return null;

            // Decode payload
            var padded = parts[0].Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var fields = raw.Split('|', 3);
            if (fields.Length < 2) return null;

            return new TrackingTokenPayload { CampaignId = fields[0], RecipientId = fields[1] };
        }
        catch
        {
            return null;
        }
    }

    private static void RecordEvent(string campaignId, TrackingEvent evt)
    {
        _events.AddOrUpdate(
            campaignId,
            _ => new List<TrackingEvent> { evt },
            (_, existing) =>
            {
                lock (existing) { existing.Add(evt); }
                return existing;
            });
        EvictIfNeeded(); // VULN-A12: bound memory
    }

    /// <summary>
    /// Increments the appropriate counter in CampaignAnalytics (fire-and-forget, swallows errors).
    /// </summary>
    private async Task PersistEventAsync(string campaignId, string eventType)
    {
        try
        {
            if (!Guid.TryParse(campaignId, out var id)) return;

            var analytics = await _db.CampaignAnalytics.FirstOrDefaultAsync(a => a.CampaignId == id);
            if (analytics is null) return;

            switch (eventType)
            {
                case "open": analytics.OpenedCount++; break;
                case "click": analytics.ClickedCount++; break;
                case "unsubscribe": analytics.UnsubscribedCount++; break;
                case "reply": break; // tracked in-memory only for now
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist tracking event {Type} for campaign {Id}", eventType, campaignId);
        }
    }
}

// ─── Models ──────────────────────────────────────────────────────────────────

public class TrackingEvent
{
    public string Type { get; set; } = ""; // open | click | reply | unsubscribe
    public string RecipientId { get; set; } = "";
    public string CampaignId { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public string? Url { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? ReplyText { get; set; }
}

public class TrackingTokenPayload
{
    public string CampaignId { get; set; } = "";
    public string RecipientId { get; set; } = "";
}

public class ReplyWebhookPayload
{
    public string? CampaignId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Subject { get; set; }
    public string? Text { get; set; }
    public string? Html { get; set; }
    public DateTime? ReceivedAt { get; set; }
}
