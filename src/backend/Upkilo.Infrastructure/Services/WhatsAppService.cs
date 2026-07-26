using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// WhatsApp service implementation using Twilio.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly AppDbContext _context;
    private readonly ISecretProvider _secretProvider;
    private readonly string _fromNumber;
    private readonly bool _isEnabled;

    public WhatsAppService(
        IConfiguration configuration,
        ILogger<WhatsAppService> logger,
        AppDbContext context,
        ISecretProvider secretProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _secretProvider = secretProvider;

        var accountSid = _secretProvider.GetSecret("Twilio:AccountSid") ?? configuration["Twilio:AccountSid"];
        var authToken = _secretProvider.GetSecret("Twilio:AuthToken") ?? configuration["Twilio:AuthToken"];
        _fromNumber = _secretProvider.GetSecret("Twilio:WhatsAppFromNumber") ?? configuration["Twilio:WhatsAppFromNumber"] ?? "";
        
        _isEnabled = !string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(_fromNumber);

        if (_isEnabled)
        {
            TwilioClient.Init(accountSid, authToken);
            _logger.LogInformation("Twilio WhatsApp service initialized");
        }
        else
        {
            _logger.LogWarning("Twilio WhatsApp service not configured - WhatsApp sending is disabled");
        }
    }

    public async Task<WhatsAppResult> SendWhatsAppAsync(Guid tenantId, string toPhoneNumber, string message, Guid? clientId = null)
    {
        if (!_isEnabled)
        {
            _logger.LogError("WhatsApp service is not enabled. Cannot send WhatsApp to {Phone}", toPhoneNumber);
            return new WhatsAppResult(false, null, "WhatsApp service is not configured");
        }

        try
        {
            // Ensure the to number is formatted for WhatsApp
            if (!toPhoneNumber.StartsWith("whatsapp:"))
            {
                toPhoneNumber = $"whatsapp:{toPhoneNumber}";
            }

            var pipeline = ResiliencePolicies.GetGenericRetryPolicy();
            
            var messageResource = await pipeline.ExecuteAsync(async (ct) => 
                await MessageResource.CreateAsync(
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(_fromNumber),
                    body: message
                )
            );

            _logger.LogInformation(
                "WhatsApp sent to {Phone}, SID: {MessageSid}",
                toPhoneNumber, messageResource.Sid);

            return new WhatsAppResult(true, messageResource.Sid, null);
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException)
        {
            _logger.LogCritical("WhatsApp Circuit is broken! Denying send request to {Phone}.", toPhoneNumber);
            return new WhatsAppResult(false, null, "WhatsApp service is currently unavailable (circuit broken)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", toPhoneNumber);
            return new WhatsAppResult(false, null, ex.Message);
        }
    }

    public async Task<WhatsAppResult> SendBookingConfirmationAsync(WhatsAppBookingData data)
    {
        var message = $"Hi {data.ClientName}, your booking for {data.ServiceName} at {data.BusinessName} is confirmed for {data.BookingDate:MMM dd} at {data.BookingTime:hh\\:mm}. Ref: {data.ConfirmationCode}";
        var result = await SendWhatsAppAsync(data.TenantId, data.PhoneNumber, message, data.ClientId);
        
        if (result.Success)
        {
            await LogCommunicationAsync(data, "Booking Confirmation", message, result.MessageId);
        }
        return result;
    }

    public async Task<WhatsAppResult> SendBookingReminderAsync(WhatsAppBookingData data)
    {
        var message = $"Reminder: You have an appointment for {data.ServiceName} tomorrow at {data.BookingTime:hh\\:mm} with {data.StaffName} at {data.BusinessName}. See you then!";
        var result = await SendWhatsAppAsync(data.TenantId, data.PhoneNumber, message, data.ClientId);
        
        if (result.Success)
        {
            await LogCommunicationAsync(data, "Booking Reminder", message, result.MessageId);
        }
        return result;
    }

    private async Task LogCommunicationAsync(WhatsAppBookingData data, string subject, string body, string? externalReference = null)
    {
        try
        {
            var log = new CommunicationLog
            {
                Id = Guid.NewGuid(),
                TenantId = data.TenantId,
                ClientId = data.ClientId,
                Type = CommunicationType.WhatsApp,
                Direction = CommunicationDirection.Outbound,
                Subject = subject,
                Body = body,
                Status = CommunicationStatus.Sent,
                ExternalReference = externalReference,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.CommunicationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log WhatsApp communication for client {ClientId}", data.ClientId);
        }
    }
}
