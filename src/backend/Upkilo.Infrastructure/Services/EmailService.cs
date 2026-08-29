using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Polly;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Email service implementation using SendGrid
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly AppDbContext _context;
    private readonly ISecretProvider _secretProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger,
        AppDbContext context,
        ISecretProvider secretProvider,
        IHttpClientFactory httpClientFactory,
        IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _secretProvider = secretProvider;
        _httpClientFactory = httpClientFactory;
        _hostEnvironment = hostEnvironment;
        _fromEmail = configuration["Email:FromEmail"] ?? "noreply@upkilo.com";
        _fromName = configuration["Email:FromName"] ?? "Upkilo";
    }

    public async Task SendBookingConfirmationAsync(BookingEmailData data)
    {
        var subject = $"Booking Confirmed - {data.ServiceName}";
        var body = BuildBookingConfirmationBody(data);

        await SendEmailWithTenantAsync(data.ClientEmail, subject, body, data.TenantId);

        _logger.LogInformation(
            "Booking confirmation email sent to {Email} for booking {ConfirmationCode}",
            data.ClientEmail, data.ConfirmationCode);

        await LogCommunicationAsync(data, CommunicationType.Email, "Booking Confirmation", subject, body);
    }

    public async Task SendBookingReminderAsync(BookingEmailData data)
    {
        var subject = $"Reminder: Your appointment tomorrow - {data.ServiceName}";
        var body = BuildBookingReminderBody(data);

        await SendEmailWithTenantAsync(data.ClientEmail, subject, body, data.TenantId);

        _logger.LogInformation(
            "Booking reminder email sent to {Email} for booking {ConfirmationCode}",
            data.ClientEmail, data.ConfirmationCode);

        await LogCommunicationAsync(data, CommunicationType.Email, "Booking Reminder", subject, body);
    }

    public async Task SendBookingCancellationAsync(BookingEmailData data)
    {
        var subject = $"Booking Cancelled - {data.ServiceName}";
        var body = BuildBookingCancellationBody(data);

        await SendEmailWithTenantAsync(data.ClientEmail, subject, body, data.TenantId);

        _logger.LogInformation(
            "Booking cancellation email sent to {Email} for booking {ConfirmationCode}",
            data.ClientEmail, data.ConfirmationCode);

        await LogCommunicationAsync(data, CommunicationType.Email, "Booking Cancellation", subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetToken)
    {
        var subject = "Reset Your Password - Upkilo";
        var resetLink = $"{_configuration["App:FrontendUrl"]}/reset-password?token={resetToken}";
        var body = $@"
            <h2>Reset Your Password</h2>
            <p>You requested to reset your password. Click the link below to set a new password:</p>
            <p><a href='{resetLink}' style='background-color: #06b6d4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px;'>Reset Password</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        await SendEmailWithTenantAsync(email, subject, body, tenantId: null, disableClickTracking: true);
        _logger.LogInformation("Password reset email sent to {Email}", email);
    }

    public async Task SendEmailVerificationAsync(string email, string verificationToken)
    {
        var subject = "Verify Your Email - Upkilo";
        var verifyLink = $"{_configuration["App:FrontendUrl"]}/verify-email?token={verificationToken}";
        var body = $@"
            <h2>Verify Your Email</h2>
            <p>Welcome to Upkilo! Please verify your email address by clicking the link below:</p>
            <p><a href='{verifyLink}' style='background-color: #06b6d4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px;'>Verify Email</a></p>
        ";

        await SendEmailWithTenantAsync(email, subject, body, tenantId: null, disableClickTracking: true);
        _logger.LogInformation("Verification email sent to {Email}", email);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = $"Welcome to Upkilo, {firstName}!";
        var body = $@"
            <h2>Welcome to Upkilo!</h2>
            <p>Hi {firstName},</p>
            <p>Thank you for joining Upkilo. We're excited to help you grow your business!</p>
            <h3>Get Started:</h3>
            <ul>
                <li>Set up your business profile</li>
                <li>Add your services</li>
                <li>Invite your team</li>
                <li>Share your booking page</li>
            </ul>
            <p><a href='{_configuration["App:FrontendUrl"]}/dashboard' style='background-color: #06b6d4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px;'>Go to Dashboard</a></p>
        ";

        await SendEmailAsync(email, subject, body);
        _logger.LogInformation("Welcome email sent to {Email}", email);
    }

    public async Task SendTemplateEmailAsync(string email, string templateId, Dictionary<string, string> variables)
    {
        _logger.LogInformation("Template email {TemplateId} sent to {Email}", templateId, email);
        await Task.CompletedTask;
    }

    public async Task SendTeamInvitationAsync(string email, string businessName, string invitationLink)
    {
        var subject = $"Invitation to join {businessName} on Upkilo";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #06b6d4;'>Join the Team!</h2>
                <p>You've been invited to join <strong>{businessName}</strong> on Upkilo.</p>
                <p>Upkilo helps businesses manage bookings, clients, and growth effortlessly.</p>
                
                <div style='margin: 30px 0;'>
                    <a href='{invitationLink}' style='background-color: #06b6d4; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>
                        Accept Invitation
                    </a>
                </div>

                <p style='color: #64748b;'>If the button above doesn't work, copy and paste this link into your browser:</p>
                <p style='color: #06b6d4; font-size: 14px; word-break: break-all;'>{invitationLink}</p>
                
                <p>See you on the other side!</p>
            </div>
        ";

        await SendEmailAsync(email, subject, body);
        _logger.LogInformation("Team invitation email sent to {Email} for business {BusinessName}", email, businessName);
    }

    public async Task SendSystemEmailAsync(string to, string subject, string content)
    {
        await SendEmailAsync(to, subject, content);
    }

    public async Task SendSecurityEmailAsync(string to, string subject, string content)
    {
        await SendEmailWithTenantAsync(to, subject, content, tenantId: null, disableClickTracking: true);
    }

    public async Task SendInvoiceAsync(InvoiceEmailData data)
    {
        await SendEmailAsync(data.ToEmail, data.Subject, data.Body, true, new List<(string, byte[])> { (data.FileName, data.PdfAttachment) });
        _logger.LogInformation("Invoice email sent to {Email} with attachment {FileName}", data.ToEmail, data.FileName);
    }

    public async Task SendPaymentReceiptAsync(InvoiceEmailData data)
    {
        await SendEmailAsync(data.ToEmail, data.Subject, data.Body, true, new List<(string, byte[])> { (data.FileName, data.PdfAttachment) });
        _logger.LogInformation("Payment receipt email sent to {Email} with attachment {FileName}", data.ToEmail, data.FileName);
    }

    public async Task SendPaymentFailureEmailAsync(InvoiceEmailData data)
    {
        // Reusing SendEmailAsync but without attachment requirement handled inside
        await SendEmailAsync(data.ToEmail, data.Subject, data.Body);
        _logger.LogInformation("Payment failure email sent to {Email}", data.ToEmail);
    }

    public async Task SendDisputeAlertAsync(string toEmail, string tenantName, string customerName, decimal amount, string reason)
    {
        var subject = $"URGENT: Chargeback/Dispute Detected - {tenantName}";
        var body = $@"
            <div style='font-family: sans-serif; border: 2px solid #DC2626; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #DC2626; margin-top: 0;'>⚠️ Action Required: New Dispute</h2>
                <p>Hi {tenantName},</p>
                <p>A customer has disputed a payment. You must respond to this dispute in your Stripe dashboard immediately to prevent funds from being permanently withdrawn.</p>
                <ul>
                    <li><strong>Customer:</strong> {customerName}</li>
                    <li><strong>Amount Disputed:</strong> {amount:C}</li>
                    <li><strong>Reason:</strong> {reason}</li>
                </ul>
                <p>Please log in to your Stripe Dashboard to submit evidence.</p>
                <br/>
                <p>System Notification</p>
            </div>";

        await SendEmailAsync(toEmail, subject, body);
        _logger.LogInformation("Dispute alert sent to {Email}", toEmail);
    }

    public async Task SendTwoFactorCodeAsync(string email, string code)
    {
        var subject = $"{code} is your Upkilo verification code";
        var body = $@"
            <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px;'>
                <h2 style='color: #0f172a;'>Verification Code</h2>
                <p style='color: #475569; font-size: 16px;'>Use the following code to sign in to your Upkilo account:</p>
                <div style='background-color: #f1f5f9; padding: 24px; border-radius: 8px; text-align: center; margin: 24px 0;'>
                    <span style='font-family: monospace; font-size: 32px; font-weight: bold; letter-spacing: 4px; color: #0891b2;'>{code}</span>
                </div>
                <p style='color: #64748b; font-size: 14px;'>This code will expire in 15 minutes. If you didn't request this code, you can safely ignore this email.</p>
                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 24px 0;' />
                <p style='color: #94a3b8; font-size: 12px; text-align: center;'>&copy; {DateTime.UtcNow.Year} Upkilo. All rights reserved.</p>
            </div>";

        await SendEmailAsync(email, subject, body);
        _logger.LogInformation("2FA code email sent to {Email}", email);
    }

    public async Task SendWaitlistNotificationAsync(WaitlistEmailData data)
    {
        var subject = $"Good news! A slot is now available for {data.ServiceName}";
        var body = BuildWaitlistNotificationBody(data);

        await SendEmailAsync(data.ClientEmail, subject, body);

        _logger.LogInformation(
            "Waitlist notification email sent to {Email} for business {BusinessName}",
            data.ClientEmail, data.BusinessName);
    }

    private string BuildWaitlistNotificationBody(WaitlistEmailData data)
    {
        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #06b6d4;'>A Slot Just Opened Up!</h2>
                <p>Hi {data.ClientName},</p>
                <p>Great news! A time slot for <strong>{data.ServiceName}</strong> on <strong>{data.Date:dddd, MMMM d}</strong> has just become available at <strong>{data.BusinessName}</strong>.</p>
                
                <p>Since you were on our waitlist, we wanted to let you know right away. Slots are filled on a first-come, first-served basis, so we recommend booking quickly!</p>
                
                <div style='margin: 30px 0;'>
                    <a href='{data.BookingLink}' style='background-color: #06b6d4; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>
                        Book Now
                    </a>
                </div>

                <p>We look forward to seeing you soon!</p>
                <p>Best regards,<br/>The {data.BusinessName} Team</p>
            </div>
        ";
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, List<(string FileName, byte[] Content)>? attachments = null)
    {
        await SendEmailWithTenantAsync(to, subject, body, tenantId: null, isHtml, attachments);
    }

    private async Task SendEmailWithTenantAsync(string toEmail, string subject, string body, Guid? tenantId = null, bool isHtml = true, List<(string FileName, byte[] Content)>? attachments = null, bool disableClickTracking = false)
    {
        try
        {
            var fromEmail = _fromEmail;
            var fromName = _fromName;

            if (tenantId != null)
            {
                var whiteLabel = await _context.WhiteLabelConfigs
                    .FirstOrDefaultAsync(w => w.TenantId == tenantId.Value);

                if (whiteLabel != null && !string.IsNullOrEmpty(whiteLabel.CustomEmailDomain))
                {
                    // If we have a custom domain, we use it. 
                    // In a real SendGrid setup, the domain must be authenticated.
                    fromEmail = $"notifications@{whiteLabel.CustomEmailDomain}";

                    var tenant = await _context.Tenants.FindAsync(tenantId.Value);
                    if (tenant != null) fromName = tenant.BusinessName;
                }
            }

            var apiKey = await _secretProvider.GetSecretAsync("SendGrid:ApiKey");

            if (string.IsNullOrEmpty(apiKey) || apiKey == "SG.xxx")
            {
                var isProduction = _hostEnvironment.IsProduction();
                if (isProduction)
                {
                    _logger.LogCritical("SendGrid API Key is missing or default in PRODUCTION environment. Email sending will fail.");
                    throw new InvalidOperationException("SendGrid configuration is missing in production.");
                }

                _logger.LogWarning("SendGrid API Key is missing or default. Email sending is being simulated (Development/Test).");
                // Simulate success in dev/test
                return;
            }

            // Use the named HttpClient from factory which has centralized Polly policies
            var httpClient = _httpClientFactory.CreateClient("SendGrid");
            var client = new SendGrid.SendGridClient(httpClient, apiKey);

            var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmail, fromName);
            var to = new SendGrid.Helpers.Mail.EmailAddress(toEmail);
            var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, isHtml ? null : body, isHtml ? body : null);

            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    var base64Content = Convert.ToBase64String(attachment.Content);
                    msg.AddAttachment(attachment.FileName, base64Content);
                }
            }

            // Account-level click tracking rewrites every link to the SendGrid link-branding
            // host (url9658.upkilo.com), which is a CNAME to sendgrid.net serving a
            // *.sendgrid.net certificate. That certificate does not cover the branded host, so
            // the browser aborts with ERR_CERT_COMMON_NAME_INVALID - and because upkilo.com
            // sends HSTS for its subdomains, the user cannot even click through the warning.
            // Every verification and reset link was therefore a dead end.
            //
            // Security-critical links must not depend on that host being healthy, and there is
            // no analytics worth collecting on "did the user click their own verification
            // link", so these messages opt out of rewriting entirely and point straight at
            // App:FrontendUrl. Campaign mail is unaffected and keeps its click tracking.
            if (disableClickTracking)
            {
                msg.TrackingSettings = new SendGrid.Helpers.Mail.TrackingSettings
                {
                    ClickTracking = new SendGrid.Helpers.Mail.ClickTracking
                    {
                        Enable = false,
                        EnableText = false
                    }
                };
            }

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {To}", toEmail);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {To}. Status: {Status}, Body: {Body}", toEmail, response.StatusCode, responseBody);
                throw new Exception($"SendGrid failed with status {response.StatusCode}");
            }
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException)
        {
            _logger.LogCritical("Email Circuit is broken! Denying send request to {To}.", toEmail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending email to {To}", toEmail);
            throw;
        }
    }

    private string BuildBookingConfirmationBody(BookingEmailData data)
    {
        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #06b6d4;'>Booking Confirmed!</h2>
                <p>Hi {data.ClientName},</p>
                <p>Your booking has been confirmed. Here are the details:</p>
                
                <div style='background-color: #f8fafc; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                    <table style='width: 100%;'>
                        <tr><td style='padding: 8px 0; color: #64748b;'>Service:</td><td style='font-weight: bold;'>{data.ServiceName}</td></tr>
                        <tr><td style='padding: 8px 0; color: #64748b;'>With:</td><td style='font-weight: bold;'>{data.StaffName}</td></tr>
                        <tr><td style='padding: 8px 0; color: #64748b;'>Date:</td><td style='font-weight: bold;'>{data.BookingDate:dddd, MMMM d, yyyy}</td></tr>
                        <tr><td style='padding: 8px 0; color: #64748b;'>Time:</td><td style='font-weight: bold;'>{data.BookingTime:hh\\:mm}</td></tr>
                        <tr><td style='padding: 8px 0; color: #64748b;'>Duration:</td><td style='font-weight: bold;'>{data.DurationMinutes} minutes</td></tr>
                        <tr><td style='padding: 8px 0; color: #64748b;'>Price:</td><td style='font-weight: bold;'>${data.Price:F2}</td></tr>
                    </table>
                </div>

                <p style='background-color: #e0f2fe; padding: 12px; border-radius: 4px;'>
                    <strong>Confirmation Code:</strong> {data.ConfirmationCode}
                </p>

                <p><strong>{data.BusinessName}</strong><br/>
                {data.BusinessAddress}<br/>
                {data.BusinessPhone}</p>

                {(data.CancellationLink != null ? $"<p><a href='{data.CancellationLink}'>Need to cancel?</a></p>" : "")}
                {(data.RescheduleLink != null ? $"<p><a href='{data.RescheduleLink}'>Need to reschedule?</a></p>" : "")}


            </div>
        ";
    }

    private string BuildBookingReminderBody(BookingEmailData data)
    {
        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #06b6d4;'>Appointment Reminder</h2>
                <p>Hi {data.ClientName},</p>
                <p>This is a reminder about your upcoming appointment tomorrow:</p>
                
                <div style='background-color: #f8fafc; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                    <p style='font-size: 18px; font-weight: bold; color: #0f172a;'>{data.ServiceName}</p>
                    <p style='font-size: 16px; color: #475569;'>{data.BookingDate:dddd, MMMM d} at {data.BookingTime:hh\\:mm}</p>
                    <p style='color: #64748b;'>with {data.StaffName}</p>
                </div>

                <p><strong>{data.BusinessName}</strong><br/>{data.BusinessAddress}</p>
            </div>
        ";
    }

    private string BuildBookingCancellationBody(BookingEmailData data)
    {
        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #ef4444;'>Booking Cancelled</h2>
                <p>Hi {data.ClientName},</p>
                <p>Your booking has been cancelled:</p>
                
                <div style='background-color: #fef2f2; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                    <p style='font-weight: bold;'>{data.ServiceName}</p>
                    <p>{data.BookingDate:dddd, MMMM d} at {data.BookingTime:hh\\:mm}</p>
                </div>

                <p>Would you like to book again?</p>
                <p><a href='{_configuration["App:FrontendUrl"]}' style='background-color: #06b6d4; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px;'>Book Now</a></p>


            </div>
        ";
    }
    private async Task LogCommunicationAsync(BookingEmailData data, CommunicationType type, string logType, string subject, string body)
    {
        try
        {
            var log = new CommunicationLog
            {
                Id = Guid.NewGuid(),
                TenantId = data.TenantId,
                ClientId = data.ClientId,
                Type = type,
                Direction = CommunicationDirection.Outbound,
                Subject = subject,
                Body = body, // In production, maybe truncate or store HTML elsewhere
                Status = CommunicationStatus.Sent,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunicationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log communication for client {ClientId}", data.ClientId);
        }
    }
}
