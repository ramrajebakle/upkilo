namespace Upkilo.Core.Entities;

/// <summary>
/// Per-user notification preferences.
/// Controls which channels receive which notification types.
/// Respects GDPR consent and user opt-out choices.
/// </summary>
public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    // Channel toggles
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool WhatsAppEnabled { get; set; } = false;

    // Notification type toggles
    public bool BookingConfirmations { get; set; } = true;
    public bool BookingReminders { get; set; } = true;
    public bool BookingCancellations { get; set; } = true;
    public bool PaymentReceipts { get; set; } = true;
    public bool MarketingEmails { get; set; } = true;
    public bool PromotionalOffers { get; set; } = true;
    public bool LoyaltyUpdates { get; set; } = true;
    public bool ReviewRequests { get; set; } = true;
    public bool SecurityAlerts { get; set; } = true;    // Cannot be disabled
    public bool SystemUpdates { get; set; } = true;

    // Timing preferences
    public string? QuietHoursStart { get; set; }   // "22:00" — no non-urgent notifications
    public string? QuietHoursEnd { get; set; }      // "08:00"
    public string? PreferredTimezone { get; set; }

    // Channel priority (comma-separated, e.g., "email,sms,push")
    public string ChannelPriority { get; set; } = "email,sms,push";

    // Sound and Badge Preferences
    public bool PlaySound { get; set; } = true;
    public string? SoundFileName { get; set; } = "default.mp3";
    public bool ShowBadge { get; set; } = true;

    // Navigation
    public User? User { get; set; }
}
