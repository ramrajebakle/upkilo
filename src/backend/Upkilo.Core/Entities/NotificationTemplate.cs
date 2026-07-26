namespace Upkilo.Core.Entities;

/// <summary>
/// Notification template: reusable, multi-channel message template
/// with variable substitution (Handlebars-style).
/// Each channel (email, SMS, push, WhatsApp) can have its own template body.
///
/// Variables: {{clientName}}, {{businessName}}, {{date}}, {{time}},
///            {{serviceName}}, {{staffName}}, {{bookingId}}, {{amount}}, {{link}}
/// </summary>
public class NotificationTemplate : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;              // e.g., "Booking Confirmation"
    public string TemplateKey { get; set; } = string.Empty;        // e.g., "booking_confirmation"
    public NotificationCategory Category { get; set; }

    // Channel-specific bodies (null = channel not used)
    public string? EmailSubject { get; set; }
    public string? EmailBody { get; set; }                          // HTML
    public string? SmsBody { get; set; }                             // Plain text, 160 chars ideal
    public string? PushTitle { get; set; }
    public string? PushBody { get; set; }
    public string? WhatsAppBody { get; set; }                       // Must conform to WhatsApp template

    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }                               // System templates can't be deleted
    public string? Variables { get; set; }                           // JSON array of available variables

    // Navigation
    public Tenant? Tenant { get; set; }
}

public enum NotificationCategory
{
    BookingConfirmation,
    BookingReminder,
    BookingCancellation,
    BookingReschedule,
    PaymentReceipt,
    PaymentFailed,
    ReviewRequest,
    WelcomeEmail,
    PasswordReset,
    TwoFactorCode,
    MarketingPromo,
    BirthdayWish,
    LoyaltyUpdate,
    WaitlistNotification,
    StaffScheduleChange,
    MaintenanceNotice,
    Custom
}
