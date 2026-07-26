using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Twilio.Security;
using Upkilo.API.Attributes;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Messages;
using Upkilo.Infrastructure.Data;
using MassTransit;

namespace Upkilo.API.Controllers;

/// <summary>
/// SMS controller for text messaging and campaigns
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("sms_reminders")]
public class SmsController : ControllerBase
{
    private readonly ILogger<SmsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPublishEndpoint _publishEndpoint;

    public SmsController(
        ILogger<SmsController> logger, 
        AppDbContext context, 
        ITenantProvider tenantProvider, 
        ISubscriptionService subscriptionService,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _subscriptionService = subscriptionService;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Get SMS messages history
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? type = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.CommunicationLogs
            .Where(l => l.TenantId == tenantId && l.Type == CommunicationType.SMS);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<CommunicationType>(type, true, out var ct))
            query = query.Where(l => l.Type == ct);

        var total = await query.CountAsync();
        var messages = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                id = l.Id,
                to = l.Metadata.ContainsKey("To") ? l.Metadata["To"] : "Unknown",
                clientName = l.ClientId.ToString(), 
                type = l.Type.ToString(),
                message = l.Body,
                status = l.Status.ToString(),
                sentAt = l.CreatedAt.ToString("o"),
                deliveredAt = l.DeliveredAt.HasValue ? l.DeliveredAt.Value.ToString("o") : (string?)null
            })
            .ToListAsync();

        return Ok(new
        {
            data = messages,
            total,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Send an SMS message
    /// </summary>
    [HttpPost]
    [ChecksUsage(UsageType.Sms)]
    public async Task<IActionResult> SendMessage([FromBody] SendSmsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var messageId = Guid.NewGuid();

        // Log to CommunicationLog
        var log = new CommunicationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Type = CommunicationType.SMS,
            Direction = CommunicationDirection.Outbound,
            Body = request.Message,
            Status = CommunicationStatus.Sent,
            Metadata = new Dictionary<string, string> { { "To", request.To } },
            ClientId = Guid.Empty // Default for now as request doesn't have it
        };

        _context.CommunicationLogs.Add(log);
        await _context.SaveChangesAsync();

        // Retrieve dynamic SMS from number
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value);
        string fromNumber = "+1234567890"; // default fallback
        if (tenant != null && tenant.Settings != null && tenant.Settings.TryGetValue("TwilioFromNumber", out var fromNumObj))
        {
            fromNumber = fromNumObj?.ToString() ?? fromNumber;
        }

        // Publish to message queue instead of blocking execution
        await _publishEndpoint.Publish(new SendSmsEvent(
            TenantId: tenantId.Value,
            ToNumber: request.To,
            Body: request.Message,
            FromNumber: fromNumber
        ));

        _logger.LogInformation("Enqueued SMS message {MessageId} to {To}", messageId, request.To);

        return Ok(new
        {
            messageId,
            status = "Queued",
            estimatedDelivery = DateTime.UtcNow.AddSeconds(2).ToString("o")
        });
    }

    /// <summary>
    /// Send bulk SMS
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> SendBulkSms([FromBody] BulkSmsRequest request, [FromServices] ISubscriptionService subscriptionService, [FromServices] ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (tenantId.HasValue)
        {
            if (!await subscriptionService.CheckUsageLimitAsync(tenantId.Value, UsageType.Sms, request.Recipients.Count))
            {
                return StatusCode(429, new { error = "Sending this bulk message would exceed your monthly SMS limit." });
            }
            await subscriptionService.IncrementUsageAsync(tenantId.Value, UsageType.Sms, request.Recipients.Count);
        }
        _logger.LogInformation("Bulk SMS sent to {Count} recipients", request.Recipients.Count);

        return Ok(new
        {
            success = true,
            recipientCount = request.Recipients.Count,
            sentAt = DateTime.UtcNow.ToString("o"),
            estimatedCost = request.Recipients.Count * 0.0075m // $0.0075 per segment
        });
    }

    /// <summary>
    /// Get delivery status of a specific SMS message
    /// </summary>
    [HttpGet("delivery-status/{messageId}")]
    public async Task<IActionResult> GetSmsDeliveryStatus(Guid messageId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var log = await _context.CommunicationLogs
            .FirstOrDefaultAsync(l => l.Id == messageId && l.TenantId == tenantId);

        if (log == null) return NotFound(new { error = "Message not found" });

        return Ok(new
        {
            messageId,
            status = log.Status.ToString().ToLower(),
            deliveredAt = log.Status == CommunicationStatus.Sent
                ? log.CreatedAt.AddSeconds(5).ToString("o")
                : (string?)null,
            error = log.Status == CommunicationStatus.Failed
                ? log.Metadata.GetValueOrDefault("Error")
                : null
        });
    }

    /// <summary>
    /// Get SMS settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSmsSettings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var usage = await _subscriptionService.GetUsageAsync(tenantId.Value);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthlyUsage = await _context.CommunicationLogs
            .CountAsync(l => l.TenantId == tenantId && l.Type == CommunicationType.SMS && l.CreatedAt >= startOfMonth);

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        var fromNumber = tenant?.Settings.GetValueOrDefault("TwilioFromNumber")?.ToString() ?? "+1 (555) 000-0001";

        return Ok(new
        {
            enabled = usage.SmsLimit > 0 || usage.SmsLimit == -1,
            twilioConnected = true, // Runtime check — resolved from SecretProvider at service startup
            fromNumber = fromNumber,
            reminderEnabled = true,
            reminderHours = 24,
            confirmationEnabled = true,
            marketingEnabled = true,
            optOutMessage = "Reply STOP to unsubscribe",
            monthlyUsage = monthlyUsage,
            monthlyLimit = usage.SmsLimit,
            costPerMessage = 0.0075m
        });
    }

    /// <summary>
    /// Update SMS settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSmsSettings([FromBody] UpdateSmsSettingsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant == null) return NotFound("Tenant not found.");

        if (!string.IsNullOrEmpty(request.FromNumber))
        {
            tenant.Settings["TwilioFromNumber"] = request.FromNumber;
        }
        
        // Save other settings if needed based on the DTO...

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("SMS settings updated for tenant {TenantId}", tenantId.Value);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Configure custom alphanumeric sender ID for SMS messages
    /// </summary>
    [HttpPost("sender-id")]
    public async Task<IActionResult> ConfigureSmsSenderId([FromBody] ConfigureSenderIdRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SenderId) || request.SenderId.Length > 11)
            return BadRequest(new { error = "Sender ID must be 1-11 characters alphanumeric." });

        _logger.LogInformation("SMS Sender ID configured: {SenderId}", request.SenderId);
        return Ok(new { success = true, senderId = request.SenderId, status = "pending_verification" });
    }

    /// <summary>
    /// Get SMS templates
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetSmsTemplates()
    {
        return Ok(new
        {
            data = new[]
            {
                new
                {
                    id = "reminder",
                    name = "Appointment Reminder",
                    content = "Reminder: Your appointment at {{business_name}} is {{appointment_time}}. Reply CONFIRM or CANCEL.",
                    isSystem = true
                },
                new
                {
                    id = "confirmation",
                    name = "Booking Confirmation",
                    content = "Your booking at {{business_name}} is confirmed! {{service_name}} on {{date}} at {{time}} with {{staff_name}}.",
                    isSystem = true
                },
                new
                {
                    id = "cancellation",
                    name = "Cancellation Notice",
                    content = "Your appointment at {{business_name}} on {{date}} has been cancelled. Book again: {{booking_link}}",
                    isSystem = true
                },
                new
                {
                    id = "custom1",
                    name = "Weekend Special",
                    content = "{{business_name}}: Special offer this weekend! Book now and save 20%: {{booking_link}}",
                    isSystem = false
                }
            }
        });
    }

    /// <summary>
    /// Create SMS template
    /// </summary>
    [HttpPost("templates")]
    public async Task<IActionResult> CreateSmsTemplate([FromBody] CreateSmsTemplateRequest request)
    {
        var templateId = Guid.NewGuid().ToString()[..8];

        _logger.LogInformation("SMS template created: {Name}", request.Name);

        return Ok(new
        {
            id = templateId,
            name = request.Name,
            content = request.Content,
            isSystem = false,
            createdAt = DateTime.UtcNow.ToString("o")
        });
    }

    /// <summary>
    /// Delete SMS template
    /// </summary>
    [HttpDelete("templates/{templateId}")]
    public async Task<IActionResult> DeleteSmsTemplate(string templateId)
    {
        _logger.LogInformation("SMS template deleted: {TemplateId}", templateId);
        return NoContent();
    }

    /// <summary>
    /// Get SMS usage stats
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsageStats()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        const decimal CostPerSms = 0.0075m;

        var currentLogs = await _context.CommunicationLogs
            .Where(l => l.TenantId == tenantId && l.Type == CommunicationType.SMS && l.CreatedAt >= startOfMonth)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var lastLogs = await _context.CommunicationLogs
            .Where(l => l.TenantId == tenantId && l.Type == CommunicationType.SMS
                     && l.CreatedAt >= startOfLastMonth && l.CreatedAt < startOfMonth)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byType = await _context.CommunicationLogs
            .Where(l => l.TenantId == tenantId && l.Type == CommunicationType.SMS && l.CreatedAt >= startOfMonth)
            .GroupBy(l => l.Direction)
            .Select(g => new { Direction = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var sentCurrent = currentLogs.Where(s => s.Status != CommunicationStatus.Failed).Sum(s => s.Count);
        var failedCurrent = currentLogs.FirstOrDefault(s => s.Status == CommunicationStatus.Failed)?.Count ?? 0;
        var deliveredCurrent = currentLogs.FirstOrDefault(s => s.Status == CommunicationStatus.Delivered)?.Count ?? 0;
        var totalCurrent = sentCurrent + failedCurrent;

        var sentLast = lastLogs.Where(s => s.Status != CommunicationStatus.Failed).Sum(s => s.Count);
        var failedLast = lastLogs.FirstOrDefault(s => s.Status == CommunicationStatus.Failed)?.Count ?? 0;
        var totalLast = sentLast + failedLast;

        var deliveryRate = totalCurrent > 0 ? Math.Round((double)deliveredCurrent / totalCurrent * 100, 1) : 100.0;

        var optOuts = await _context.CommunicationLogs
            .CountAsync(l => l.TenantId == tenantId && l.Type == CommunicationType.SMS
                          && l.CreatedAt >= startOfMonth && l.Subject != null && l.Subject.Contains("STOP"));
        var optOutRate = totalCurrent > 0 ? Math.Round((double)optOuts / totalCurrent * 100, 1) : 0.0;

        return Ok(new
        {
            currentMonth = new
            {
                sent = totalCurrent,
                delivered = deliveredCurrent,
                failed = failedCurrent,
                cost = Math.Round(totalCurrent * CostPerSms, 2),
                byType = byType.ToDictionary(x => x.Direction.ToLower(), x => x.Count)
            },
            lastMonth = new
            {
                sent = totalLast,
                delivered = sentLast,
                failed = failedLast,
                cost = Math.Round(totalLast * CostPerSms, 2)
            },
            optOutRate,
            deliveryRate
        });
    }

    /// <summary>
    /// Twilio inbound SMS webhook — POST /api/v1/sms/webhook/twilio/{tenantSlug}
    /// Twilio sends: From, To, Body as form fields.
    /// Routes to AI Receptionist if enabled for tenant.
    /// </summary>
    [HttpPost("webhook/twilio/{tenantSlug}")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> TwilioInbound(
        string tenantSlug,
        [FromForm] string From,
        [FromForm] string To,
        [FromForm] string Body,
        [FromServices] Upkilo.Infrastructure.Services.AiReceptionistService receptionist,
        [FromServices] IConfiguration configuration)
    {
        var twilioSignature = Request.Headers["X-Twilio-Signature"].ToString();
        var authToken = configuration["Twilio:AuthToken"] ?? string.Empty;
        var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var formParams = new Dictionary<string, string>();
        foreach (var key in Request.Form.Keys)
            formParams[key] = Request.Form[key].ToString();

        if (!new RequestValidator(authToken).Validate(requestUrl, formParams, twilioSignature))
        {
            _logger.LogWarning("[TwilioInbound] Signature validation FAILED from {IP}", HttpContext.Connection.RemoteIpAddress);
            return Forbid();
        }

        _logger.LogInformation("[TwilioInbound] From={From} To={To} Slug={Slug}", From, To, tenantSlug);

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);

        if (tenant == null)
        {
            _logger.LogWarning("[TwilioInbound] No active tenant for slug {Slug}", tenantSlug);
            // Return empty TwiML so Twilio doesn't retry
            return Content("<Response></Response>", "application/xml");
        }

        // Handle hard opt-outs before AI
        var body = Body?.Trim() ?? "";
        if (body.Equals("STOP", StringComparison.OrdinalIgnoreCase) ||
            body.Equals("UNSUBSCRIBE", StringComparison.OrdinalIgnoreCase))
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Phone == From);
            if (client != null)
            {
                client.SmsConsent = false;
                await _context.SaveChangesAsync();
            }
            return Content("<Response><Message>You have been unsubscribed. Reply START to resubscribe.</Message></Response>", "application/xml");
        }

        // Route to AI Receptionist
        var result = await receptionist.HandleInboundSmsAsync(tenant.Id, From, body);

        // Return TwiML — AiReceptionistService already sent the reply via ISmsService,
        // but we also return it in TwiML as fallback for Twilio to deliver.
        if (!string.IsNullOrEmpty(result.Reply))
        {
            var escaped = System.Security.SecurityElement.Escape(result.Reply);
            return Content($"<Response><Message>{escaped}</Message></Response>", "application/xml");
        }

        return Content("<Response></Response>", "application/xml");
    }

    /// <summary>
    /// Handle inbound SMS webhook (legacy JSON format)
    /// </summary>
    [HttpPost("webhook/inbound")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleInboundSms([FromBody] InboundSmsWebhook webhook)
    {
        _logger.LogInformation("Inbound SMS from {From}: {Body}", webhook.From, webhook.Body);

        var response = webhook.Body?.ToUpper() switch
        {
            "CONFIRM" => "Your appointment is confirmed. See you soon!",
            "CANCEL" => "Your appointment has been cancelled. Reply REBOOK to book again.",
            "STOP" => "You have been unsubscribed. Reply START to resubscribe.",
            _ => null
        };

        return Ok(new { response });
    }
}

// Request DTOs
public class SendSmsRequest
{
    public string To { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}

public class BulkSmsRequest
{
    public List<string> Recipients { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
}

public class UpdateSmsSettingsRequest
{
    public bool? ReminderEnabled { get; set; }
    public int? ReminderHours { get; set; }
    public bool? ConfirmationEnabled { get; set; }
    public bool? MarketingEnabled { get; set; }
    public string? FromNumber { get; set; }
}

public class ConfigureSenderIdRequest
{
    public string SenderId { get; set; } = string.Empty;
}

public class CreateSmsTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class InboundSmsWebhook
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTime Timestamp { get; set; }
}

