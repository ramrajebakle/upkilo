using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly ILogger<SmsService> _logger;
    private readonly AppDbContext _context;
    private readonly ISecretProvider _secretProvider;
    private readonly bool _isEnabled;

    public SmsService(IConfiguration configuration, ILogger<SmsService> logger, AppDbContext context, ISecretProvider secretProvider)
    {
        _logger = logger;
        _context = context;
        _secretProvider = secretProvider;

        _accountSid = _secretProvider.GetSecret("Twilio:AccountSid") ?? configuration["Twilio:AccountSid"] ?? string.Empty;
        _authToken = _secretProvider.GetSecret("Twilio:AuthToken") ?? configuration["Twilio:AuthToken"] ?? string.Empty;

        // Reads Twilio:PhoneNumber, which is the key that actually exists.
        //
        // This read Twilio:FromNumber, and that key was defined NOWHERE - not in appsettings.json,
        // not in deploy.yml, not in .env.example, not in App Service config. The only occurrence
        // in the repository was the line reading it. So _fromNumber was the empty string in every
        // environment, always.
        //
        // Twilio:FromNumber is still honoured first so any environment that did set it keeps
        // working, but the fallback is the key the rest of the system uses.
        _fromNumber = _secretProvider.GetSecret("Twilio:FromNumber")
                      ?? configuration["Twilio:FromNumber"]
                      ?? _secretProvider.GetSecret("Twilio:PhoneNumber")
                      ?? configuration["Twilio:PhoneNumber"]
                      ?? string.Empty;

        // The sending number is part of being configured.
        //
        // It was excluded, so with credentials present but no number the service reported itself
        // ENABLED and then called Twilio with from: "". Every send failed at the API, one message
        // at a time, instead of the service saying plainly that it was not set up. That is the
        // worse of the two failure modes: WhatsApp, which does check its number, at least refuses
        // honestly and says why.
        _isEnabled = !string.IsNullOrEmpty(_accountSid)
                     && !string.IsNullOrEmpty(_authToken)
                     && !string.IsNullOrEmpty(_fromNumber);

        if (_isEnabled)
        {
            TwilioClient.Init(_accountSid, _authToken);
        }
    }

    public async Task<SmsResult> SendSmsAsync(Guid tenantId, string to, string message, Guid? clientId = null)
    {
        if (!_isEnabled)
        {
            // Names the missing piece. "Missing Twilio credentials" sent whoever read it to check
            // the account SID and auth token, which in production were both present — the actual
            // gap was the sending number.
            var missing = string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken)
                ? "Twilio:AccountSid / Twilio:AuthToken"
                : "Twilio:PhoneNumber (sending number)";

            _logger.LogWarning("SMS Service is disabled. Not configured: {Missing}.", missing);
            return new SmsResult(false, null, "SMS Service is disabled");
        }

        try
        {
            using var smsCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(_fromNumber),
                to: new Twilio.Types.PhoneNumber(to)
            );

            _logger.LogInformation("SMS sent to {To}. SID: {Sid}", to, messageResource.Sid);

            // Log communication
            var log = new CommunicationLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ClientId = clientId,
                Type = CommunicationType.SMS,
                Direction = CommunicationDirection.Outbound,
                Body = message,
                Status = CommunicationStatus.Sent,
                ReferenceId = messageResource.Sid,
                Metadata = new Dictionary<string, string> { { "Provider", "Twilio" } },
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunicationLogs.Add(log);
            using var saveCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _context.SaveChangesAsync(saveCts.Token);

            return new SmsResult(true, messageResource.Sid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", to);
            return new SmsResult(false, null, ex.Message);
        }
    }

    public async Task<SmsResult> SendBookingConfirmationAsync(Booking booking)
    {
        var client = await _context.Clients.FindAsync(booking.ClientId);
        if (client == null || string.IsNullOrEmpty(client.Phone))
            return new SmsResult(false, null, "Client phone not found");

        var template = await GetTemplateAsync(booking.TenantId, NotificationCategory.BookingConfirmation);
        var message = RenderTemplate(template?.SmsBody ?? "Booking confirmed for {{date}} at {{time}}", booking, client);

        return await SendSmsAsync(booking.TenantId, client.Phone, message, client.Id);
    }

    public async Task<SmsResult> SendBookingReminderAsync(Booking booking)
    {
        var client = await _context.Clients.FindAsync(booking.ClientId);
        if (client == null || string.IsNullOrEmpty(client.Phone))
            return new SmsResult(false, null, "Client phone not found");

        var template = await GetTemplateAsync(booking.TenantId, NotificationCategory.BookingReminder);
        var message = RenderTemplate(template?.SmsBody ?? "Reminder: Booking scheduled for {{date}} at {{time}}", booking, client);

        return await SendSmsAsync(booking.TenantId, client.Phone, message, client.Id);
    }

    public async Task<SmsResult> SendBookingCancellationAsync(Booking booking)
    {
        var client = await _context.Clients.FindAsync(booking.ClientId);
        if (client == null || string.IsNullOrEmpty(client.Phone))
            return new SmsResult(false, null, "Client phone not found");

        var template = await GetTemplateAsync(booking.TenantId, NotificationCategory.BookingCancellation);
        var message = RenderTemplate(template?.SmsBody ?? "Booking on {{date}} has been cancelled.", booking, client);

        return await SendSmsAsync(booking.TenantId, client.Phone, message, client.Id);
    }

    public async Task<SmsResult> SendVerificationCodeAsync(Guid tenantId, string phoneNumber, string code)
    {
        return await SendSmsAsync(tenantId, phoneNumber, $"Your Upkilo verification code is: {code}");
    }

    private async Task<NotificationTemplate?> GetTemplateAsync(Guid tenantId, NotificationCategory category)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Category == category && t.IsActive);
    }

    private string RenderTemplate(string template, Booking booking, Client client)
    {
        return template
            .Replace("{{clientName}}", client.FirstName)
            .Replace("{{date}}", booking.StartTime.ToShortDateString())
            .Replace("{{time}}", booking.StartTime.ToShortTimeString())
            .Replace("{{serviceName}}", "your appointment"); // Ideally fetch service name
    }
}
