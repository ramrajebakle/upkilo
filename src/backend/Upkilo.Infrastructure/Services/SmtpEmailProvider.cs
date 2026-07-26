using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// SMTP email provider — configurable fallback when SendGrid is unavailable.
/// Supports any SMTP server (Gmail, Office 365, Amazon SES SMTP, self-hosted).
/// Configuration keys:
///   Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password, Smtp:EnableSsl, Smtp:FromEmail, Smtp:FromName
/// </summary>
public class SmtpEmailProvider
{
    private readonly ILogger<SmtpEmailProvider> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _enableSsl;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _isConfigured;

    public SmtpEmailProvider(IConfiguration configuration, ILogger<SmtpEmailProvider> logger)
    {
        _logger = logger;
        _host = configuration["Smtp:Host"] ?? "";
        _port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 587;
        _username = configuration["Smtp:Username"] ?? "";
        _password = configuration["Smtp:Password"] ?? "";
        _enableSsl = configuration["Smtp:EnableSsl"] != "false";
        _fromEmail = configuration["Smtp:FromEmail"] ?? configuration["Email:FromEmail"] ?? "noreply@upkilo.com";
        _fromName = configuration["Smtp:FromName"] ?? configuration["Email:FromName"] ?? "Upkilo";

        _isConfigured = !string.IsNullOrEmpty(_host) && !string.IsNullOrEmpty(_username);

        if (_isConfigured)
            _logger.LogInformation("SMTP email provider initialized: {Host}:{Port}", _host, _port);
        else
            _logger.LogWarning("SMTP not configured — SMTP fallback disabled");
    }

    public bool IsConfigured => _isConfigured;

    /// <summary>
    /// Send an email via SMTP. Returns true if sent successfully.
    /// </summary>
    public async Task<bool> SendAsync(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("SMTP not configured, cannot send email to {To}", to);
            return false;
        }

        try
        {
            using var client = new SmtpClient(_host, _port)
            {
                Credentials = new NetworkCredential(_username, _password),
                EnableSsl = _enableSsl,
                Timeout = 30000 // 30s timeout
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(to);

            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                var stream = new MemoryStream(attachment);
                message.Attachments.Add(new Attachment(stream, attachmentName, "application/pdf"));
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("SMTP email sent to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed to {To}: {Subject}", to, subject);
            return false;
        }
    }
}
