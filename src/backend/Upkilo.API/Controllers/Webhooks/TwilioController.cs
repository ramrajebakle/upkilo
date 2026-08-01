using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Twilio.Security;
using Upkilo.API.Services;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks/twilio")]
public class TwilioController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TwilioController> _logger;
    private readonly IChatbotService _chatbotService;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _configuration;

    public TwilioController(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<TwilioController> logger,
        IChatbotService chatbotService,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        IConfiguration configuration)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _chatbotService = chatbotService;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _configuration = configuration;
    }

    [HttpPost("sms")]
    public async Task<IActionResult> ReceiveSms([FromForm] string From, [FromForm] string Body, [FromForm] string To, [FromForm] string MessageSid)
    {
        // Verify Twilio HMAC signature to prevent webhook spoofing
        var authToken = _configuration["Twilio:AuthToken"];
        if (!string.IsNullOrEmpty(authToken))
        {
            var validator = new RequestValidator(authToken);
            var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            var parameters = new Dictionary<string, string>
            {
                ["From"] = From ?? "",
                ["Body"] = Body ?? "",
                ["To"] = To ?? "",
                ["MessageSid"] = MessageSid ?? ""
            };
            var signature = Request.Headers["X-Twilio-Signature"].ToString();
            if (!validator.Validate(requestUrl, parameters, signature))
            {
                _logger.LogWarning("SECURITY: Invalid Twilio signature from {MessageSid}", MessageSid);
                return Forbid();
            }
        }

        // Log MessageSid only — full body may contain PII/health content
        _logger.LogInformation("Received SMS sid={MessageSid} from={From}", MessageSid, From);

        try
        {
            var isWhatsApp = From.StartsWith("whatsapp:");
            var normalizedPhone = From.Replace("whatsapp:", "").Replace("+", "");

            var client = await _context.Clients
                .Where(c => c.Phone == normalizedPhone || c.Phone == "+" + normalizedPhone)
                .FirstOrDefaultAsync();

            if (client != null)
            {
                var log = new CommunicationLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = client.TenantId,
                    ClientId = client.Id,
                    Type = isWhatsApp ? CommunicationType.WhatsApp : CommunicationType.SMS,
                    Direction = CommunicationDirection.Inbound,
                    Body = Body,
                    Status = CommunicationStatus.Received,
                    ExternalReference = MessageSid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<CommunicationLog>().Add(log);
                await _context.SaveChangesAsync();

                // 2. Trigger AI Chatbot
                var chatRequest = new ChatRequestDto
                {
                    TenantId = client.TenantId,
                    Message = Body,
                    ExternalId = client.Id.ToString(),
                    Channel = isWhatsApp ? ConversationChannel.WhatsApp : ConversationChannel.SMS
                };

                var aiResponse = await _chatbotService.ProcessMessageAsync(chatRequest);

                // 3. Send AI response back
                if (aiResponse != null && !string.IsNullOrEmpty(aiResponse.Response))
                {
                    if (isWhatsApp)
                    {
                        await _whatsAppService.SendWhatsAppAsync(client.TenantId, From.Replace("whatsapp:", ""), aiResponse.Response, client.Id);
                    }
                    else
                    {
                        await _smsService.SendSmsAsync(client.TenantId, From, aiResponse.Response, client.Id);
                    }
                }

                // Notify Staff (In background)
                await _notificationService.SendToTenantAsync(client.TenantId.ToString(), "MessageProcessedByAI", new
                {
                    clientId = client.Id,
                    clientName = $"{client.FirstName} {client.LastName}",
                    body = Body,
                    aiResponse = aiResponse?.Response,
                    channel = isWhatsApp ? "WhatsApp" : "SMS"
                });
            }
            else
            {
                _logger.LogWarning("Received message from unknown number: {From}", From);
                // Optional: Handle unknown leads here
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming Twilio message from {From}", From);
        }

        return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response />", "application/xml");
    }
}
