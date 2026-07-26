using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Notification channel orchestrator — sends notifications via primary channel
/// and automatically falls back to secondary channels on failure.
/// 
/// Priority order (configurable per tenant):
///   1. Email (SendGrid) → fallback to SMTP
///   2. SMS (Twilio)
///   3. WhatsApp (future)
///
/// Example: If email fails, retry via SMTP. If SMTP also fails, try SMS.
/// </summary>
public class NotificationFallbackService
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly SmtpEmailProvider _smtpProvider;
    private readonly ILogger<NotificationFallbackService> _logger;

    public NotificationFallbackService(
        IEmailService emailService,
        ISmsService smsService,
        SmtpEmailProvider smtpProvider,
        ILogger<NotificationFallbackService> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _smtpProvider = smtpProvider;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification using the best available channel.
    /// Falls back to alternatives if the primary channel fails.
    /// </summary>
    public async Task<NotificationResult> SendAsync(
        Guid tenantId,
        string? email,
        string? phone,
        string subject,
        string htmlBody,
        string? smsBody = null,
        Guid? clientId = null)
    {
        var channels = new List<string>();

        // Channel 1: Email via SendGrid
        if (!string.IsNullOrEmpty(email))
        {
            try
            {
                await _emailService.SendSystemEmailAsync(email, subject, htmlBody);
                channels.Add("email");
                _logger.LogInformation("Notification sent via email to {Email}", email);
                return new NotificationResult(true, "email", channels);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email send failed for {Email}, trying SMTP fallback", email);
                channels.Add("email:failed");
            }

            // Channel 1b: SMTP Fallback
            if (_smtpProvider.IsConfigured)
            {
                var smtpSent = await _smtpProvider.SendAsync(email, subject, htmlBody);
                if (smtpSent)
                {
                    channels.Add("smtp");
                    _logger.LogInformation("Notification sent via SMTP fallback to {Email}", email);
                    return new NotificationResult(true, "smtp", channels);
                }
                channels.Add("smtp:failed");
            }
        }

        // Channel 2: SMS via Twilio
        if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(smsBody))
        {
            try
            {
                var result = await _smsService.SendSmsAsync(tenantId, phone, smsBody, clientId);
                if (result.Success)
                {
                    channels.Add("sms");
                    _logger.LogInformation("Notification sent via SMS to {Phone}", phone);
                    return new NotificationResult(true, "sms", channels);
                }
                channels.Add($"sms:failed:{result.Error}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMS send failed for {Phone}", phone);
                channels.Add("sms:error");
            }
        }

        _logger.LogError("All notification channels exhausted. Subject: {Subject}", subject);
        return new NotificationResult(false, null, channels);
    }
}

/// <summary>
/// Result of a multi-channel notification attempt
/// </summary>
public record NotificationResult(
    bool Success,
    string? DeliveredVia,
    List<string> ChannelsAttempted
);
