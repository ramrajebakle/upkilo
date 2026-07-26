namespace Upkilo.Core.Interfaces;

/// <summary>
/// Email service interface for sending transactional and marketing emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a booking confirmation email to the client
    /// </summary>
    Task SendBookingConfirmationAsync(BookingEmailData data);

    /// <summary>
    /// Send a booking reminder email to the client
    /// </summary>
    Task SendBookingReminderAsync(BookingEmailData data);

    /// <summary>
    /// Send a booking cancellation email to the client
    /// </summary>
    Task SendBookingCancellationAsync(BookingEmailData data);

    /// <summary>
    /// Send a password reset email
    /// </summary>
    Task SendPasswordResetAsync(string email, string resetToken);

    /// <summary>
    /// Send an email verification email
    /// </summary>
    Task SendEmailVerificationAsync(string email, string verificationToken);

    /// <summary>
    /// Send a welcome email to new users
    /// </summary>
    Task SendWelcomeEmailAsync(string email, string firstName);

    /// <summary>
    /// Send a custom email using a template
    /// </summary>
    /// <summary>
    /// Send a team invitation email
    /// </summary>
    Task SendTeamInvitationAsync(string email, string businessName, string invitationLink);

    /// <summary>
    /// Send a system email (generic)
    /// </summary>
    Task SendSystemEmailAsync(string to, string subject, string content);

    /// <summary>
    /// Send an invoice with PDF attachment
    /// </summary>
    Task SendInvoiceAsync(InvoiceEmailData data);

    /// <summary>
    /// Send payment failure notification
    /// </summary>
    Task SendPaymentFailureEmailAsync(InvoiceEmailData data);

    /// <summary>
    /// Send a payment receipt with PDF attachment
    /// </summary>
    Task SendPaymentReceiptAsync(InvoiceEmailData data);

    /// <summary>
    /// Send urgent dispute alert to tenant owner
    /// </summary>
    Task SendDisputeAlertAsync(string toEmail, string tenantName, string customerName, decimal amount, string reason);

    /// <summary>
    /// Send a two-factor authentication code
    /// </summary>
    Task SendTwoFactorCodeAsync(string email, string code);

    /// <summary>
    /// Send a generic email with optional attachments
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, List<(string FileName, byte[] Content)>? attachments = null);

    /// <summary>
    /// Send a waitlist availability notification
    /// </summary>
    Task SendWaitlistNotificationAsync(WaitlistEmailData data);
}

/// <summary>
/// Data for booking-related emails
/// </summary>
public record BookingEmailData(
    string ClientEmail,
    Guid ClientId,
    Guid TenantId,
    string ClientName,
    string ServiceName,
    string StaffName,
    DateTime BookingDate,
    TimeSpan BookingTime,
    int DurationMinutes,
    decimal Price,
    string ConfirmationCode,
    string BusinessName,
    string BusinessAddress,
    string BusinessPhone,
    string? CancellationLink = null,
    string? RescheduleLink = null
);

/// <summary>
/// Data for invoice-related emails
/// </summary>
public record InvoiceEmailData(
    string ToEmail,
    string ToName,
    string Subject,
    string Body,
    byte[] PdfAttachment,
    string FileName
);

/// <summary>
/// Data for waitlist-related emails
/// </summary>
public record WaitlistEmailData(
    string ClientEmail,
    string ClientName,
    string ServiceName,
    DateTime Date,
    string BookingLink,
    string BusinessName
);
