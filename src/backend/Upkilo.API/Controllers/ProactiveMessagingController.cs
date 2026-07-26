using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Attributes;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Proactive Messaging �� AI-driven outbound client communications.
/// Preview, approve, and send personalized messages based on behavioral triggers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proactive-messaging")]
[Authorize]
[FeatureGuard("ai_copilot")]
public class ProactiveMessagingController : ControllerBase
{
    private readonly IProactiveMessagingService _messaging;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ProactiveMessagingController> _logger;
    private readonly AppDbContext _db;

    public ProactiveMessagingController(
        IProactiveMessagingService messaging,
        ITenantProvider tenantProvider,
        ILogger<ProactiveMessagingController> logger,
        AppDbContext db)
    {
        _messaging = messaging;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _db = db;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/proactive-messaging/preview
    // Returns all pending messages that would be sent — preview before sending
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var messages = await _messaging.GeneratePendingMessagesAsync(tenantId.Value, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            messages,
            total = messages.Count,
            byTrigger = messages.GroupBy(m => m.Trigger)
                .Select(g => new { trigger = g.Key, count = g.Count() })
                .ToList(),
            generatedAt = DateTime.UtcNow
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/proactive-messaging/send
    // Sends all immediately due proactive messages
    // Body: { dryRun: true } to preview without actually sending
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(
        [FromBody] SendMessagesRequest req,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var sent = await _messaging.SendPendingMessagesAsync(tenantId.Value, req.DryRun, ct);

        _logger.LogInformation(
            "Proactive messages sent: {Count} (dryRun={DryRun}) tenant={TenantId}",
            sent, req.DryRun, tenantId);

        return Ok(ApiResponse<object>.Ok(new
        {
            sent,
            dryRun = req.DryRun,
            message = req.DryRun
                ? $"Dry run: {sent} message(s) would be sent"
                : $"{sent} message(s) sent successfully"
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/proactive-messaging/generate
    // Generate a message for a specific client + trigger (preview or send)
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateMessageRequest req,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var validTriggers = new[] { "lapsed_client", "birthday", "post_service", "milestone", "custom" };
        if (!validTriggers.Contains(req.Trigger))
            return BadRequest(ApiResponse<object>.Fail($"Invalid trigger. Valid: {string.Join(", ", validTriggers)}"));

        var message = await _messaging.GenerateForClientAsync(
            tenantId.Value, req.ClientId, req.Trigger, ct);

        if (message == null)
            return NotFound(ApiResponse<object>.Fail("Client not found"));

        return Ok(ApiResponse<object>.Ok(message));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/proactive-messaging/win-back/stats
    // Returns live win-back opportunity stats — how many lapsed clients exist,
    // their average lifetime value, and estimated revenue if they return once.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("win-back/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> WinBackStats(
        [FromQuery] int lapsedDays = 60,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        lapsedDays = Math.Clamp(lapsedDays, 14, 365);
        var cutoff = DateTime.UtcNow.AddDays(-lapsedDays);

        var lapsed = await _db.Clients
            .Where(c => c.TenantId == tenantId.Value
                     && c.MarketingConsent
                     && (c.LastVisitAt == null || c.LastVisitAt < cutoff))
            .Select(c => new { c.LifetimeValue })
            .ToListAsync(ct);

        var count    = lapsed.Count;
        var avgLtv   = count > 0 ? Math.Round(lapsed.Average(c => (double)c.LifetimeValue), 2) : 0;
        // Estimate one additional visit = ~25% of their avg order value (LTV / typical visits)
        var estimatedRecovery = count > 0
            ? Math.Round(lapsed.Sum(c => (double)c.LifetimeValue * 0.10), 2)
            : 0;

        return Ok(ApiResponse<object>.Ok(new
        {
            lapsedDays,
            lapsedClientCount    = count,
            avgLifetimeValue     = avgLtv,
            estimatedRecoveryRevenue = estimatedRecovery,
            message = count > 0
                ? $"You have {count} lapsed clients — a win-back campaign could recover ~${estimatedRecovery:F0}."
                : "No lapsed clients found. Great retention!"
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/proactive-messaging/triggers
    // Returns available triggers and their descriptions
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("triggers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetTriggers()
    {
        var triggers = new[]
        {
            new { id = "lapsed_client", name = "Lapsed Client", description = "Client hasn't booked in 60+ days", icon = "⏰" },
            new { id = "birthday", name = "Birthday", description = "Client birthday within 7 days", icon = "🎂" },
            new { id = "post_service_followup", name = "Post-Service Follow-up", description = "24h after completed appointment", icon = "⭐" },
            new { id = "milestone", name = "Booking Milestone", description = "10th, 25th, 50th booking", icon = "🏆" },
            new { id = "abandoned_booking", name = "Abandoned Booking", description = "Started but didn't complete a booking", icon = "🛒" },
        };

        return Ok(ApiResponse<object>.Ok(new { triggers }));
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record SendMessagesRequest
{
    public bool DryRun { get; init; } = true;
}

public record GenerateMessageRequest
{
    public Guid ClientId { get; init; }
    public string Trigger { get; init; } = "lapsed_client";
}
