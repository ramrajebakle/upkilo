using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Text;

namespace Upkilo.API.Controllers;

/// <summary>
/// Twilio voice webhook handler — powers the AI Voice Agent booking add-on.
/// Tenants provision a Twilio number and point its webhook at POST /api/twilio-voice/incoming.
/// The controller resolves the tenant by matching the called Twilio number against tenant settings.
/// A2: status-callback generates post-call summary + confirmation SMS.
/// A3: no-show risk > 70% triggers automatic deposit request.
/// </summary>
[ApiController]
[Route("api/twilio-voice")]
public class TwilioVoiceController : ControllerBase
{
    private readonly VoiceAgentService _voiceAgentService;
    private readonly AppDbContext _context;
    private readonly ILogger<TwilioVoiceController> _logger;

    public TwilioVoiceController(
        VoiceAgentService voiceAgentService,
        AppDbContext context,
        ILogger<TwilioVoiceController> logger)
    {
        _voiceAgentService = voiceAgentService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("incoming")]
    public async Task<IActionResult> HandleIncomingCall(
        [FromForm] string? To,
        [FromForm] string? From,
        [FromForm] string? CallSid)
    {
        var tenantId = await ResolveTenantFromPhoneAsync(To);
        _logger.LogInformation("[VoiceAgent] Incoming call To={To} From={From} CallSid={CallSid} TenantId={TenantId}",
            To, From, CallSid, tenantId);

        var businessName = "our business";
        if (tenantId.HasValue)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId.Value);
            businessName = tenant?.Name ?? businessName;
        }

        var webhookBase = $"{Request.Scheme}://{Request.Host}";
        var statusCallbackUrl = $"{webhookBase}/api/twilio-voice/status-callback?tenantId={tenantId}&callerPhone={Uri.EscapeDataString(From ?? "")}";

        var twiml = $@"<Response>
    <Say voice=""Polly.Joanna"">Hello! You've reached {EscapeXml(businessName)}. I'm the AI booking assistant. How can I help you today?</Say>
    <Gather input=""speech"" action=""/api/twilio-voice/respond?tenantId={tenantId}&amp;callSid={EscapeXml(CallSid ?? "")}"" method=""POST"" speechTimeout=""auto"" speechModel=""phone_call"" />
</Response>";

        return Content(twiml, "application/xml", Encoding.UTF8);
    }

    [HttpPost("respond")]
    public async Task<IActionResult> RespondToCall(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? callSid,
        [FromForm] string? SpeechResult)
    {
        var speech = SpeechResult ?? string.Empty;
        var resolvedTenantId = tenantId ?? Guid.Empty;
        var resolvedCallSid = callSid ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation("[VoiceAgent] Speech for tenant {TenantId} CallSid={CallSid}: {Speech}",
            resolvedTenantId, resolvedCallSid, speech[..Math.Min(50, speech.Length)]);

        var aiResponse = resolvedTenantId != Guid.Empty
            ? await _voiceAgentService.ProcessVoiceRequestAsync(resolvedTenantId, speech, resolvedCallSid)
            : "I'm sorry, I couldn't identify the business. Please call back and try again.";

        var twiml = $@"<Response>
    <Say voice=""Polly.Joanna"">{EscapeXml(aiResponse)}</Say>
    <Gather input=""speech"" action=""/api/twilio-voice/respond?tenantId={resolvedTenantId}&amp;callSid={EscapeXml(resolvedCallSid)}"" method=""POST"" speechTimeout=""auto"" speechModel=""phone_call"" />
</Response>";

        return Content(twiml, "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// A2/A3: Twilio calls this when a call ends (set statusCallbackUrl in TwiML).
    /// Generates post-call summary, sends booking confirmation SMS, scores no-show risk.
    /// </summary>
    [HttpPost("status-callback")]
    public async Task<IActionResult> StatusCallback(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? callerPhone,
        [FromForm] string? CallSid,
        [FromForm] string? CallStatus,
        [FromForm] int? CallDuration)
    {
        _logger.LogInformation("[VoiceAgent] StatusCallback TenantId={TenantId} CallSid={CallSid} Status={Status} Duration={Duration}s",
            tenantId, CallSid, CallStatus, CallDuration);

        if (tenantId.HasValue && CallStatus == "completed" && !string.IsNullOrEmpty(CallSid))
        {
            await _voiceAgentService.HandleCallCompletedAsync(
                tenantId.Value,
                CallSid,
                callerPhone ?? string.Empty,
                CallDuration ?? 0);
        }

        return Ok();
    }

    /// <summary>
    /// GET /api/twilio-voice/setup — returns setup instructions for the Voice Agent add-on.
    /// </summary>
    [HttpGet("setup")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetSetupInstructions()
    {
        var tenantIdClaim = User.FindFirst("tenantId")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound();

        var isConfigured = tenant.Settings.TryGetValue("twilio_phone_number", out var existing);
        var webhookBase = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            enabled = isConfigured,
            configuredPhoneNumber = isConfigured ? existing : null,
            webhookUrl = $"{webhookBase}/api/twilio-voice/incoming",
            statusCallbackUrl = $"{webhookBase}/api/twilio-voice/status-callback",
            setupInstructions = new[]
            {
                "1. Purchase a Twilio phone number at console.twilio.com",
                "2. In Voice webhook, set HTTP POST to the webhookUrl above",
                "3. In Status Callback, set HTTP POST to the statusCallbackUrl above",
                "4. Save your phone number in Settings → Integrations → Voice Agent"
            },
            pricing = "$29/mo add-on · Included in Business plan and above",
            features = new[] {
                "AI answers calls 24/7",
                "Books appointments autonomously",
                "Post-call summary sent to staff",
                "Booking confirmation SMS to caller",
                "No-show risk scoring with automatic deposit requests"
            }
        });
    }

    /// <summary>
    /// POST /api/twilio-voice/configure — save the tenant's Twilio phone number.
    /// </summary>
    [HttpPost("configure")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Configure([FromBody] VoiceAgentConfigRequest request)
    {
        var tenantIdClaim = User.FindFirst("tenantId")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound();

        tenant.Settings["twilio_phone_number"] = request.PhoneNumber.Trim();
        tenant.Settings["voice_agent_enabled"] = true;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("[VoiceAgent] Configured for tenant {TenantId}: {PhoneNumber}", tenantId, request.PhoneNumber);

        return Ok(new { configured = true, phoneNumber = request.PhoneNumber });
    }

    private async Task<Guid?> ResolveTenantFromPhoneAsync(string? toNumber)
    {
        if (string.IsNullOrEmpty(toNumber)) return null;
        var normalizedTo = toNumber.Trim();

        var tenants = await _context.Tenants
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.Settings })
            .ToListAsync();

        var match = tenants.FirstOrDefault(t =>
            t.Settings.TryGetValue("twilio_phone_number", out var phone)
            && phone?.ToString() == normalizedTo);

        return match?.Id;
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

public record VoiceAgentConfigRequest(string PhoneNumber);
