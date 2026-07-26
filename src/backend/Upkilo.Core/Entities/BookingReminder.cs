namespace Upkilo.Core.Entities;

/// <summary>
/// Scheduled booking reminder.
/// Each booking can have multiple reminders at different intervals.
/// Processed by BookingReminderJob.
/// </summary>
public class BookingReminder : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid BookingId { get; set; }
    public Guid ClientId { get; set; }
    public ReminderType Type { get; set; } = ReminderType.Email;
    public ReminderTiming Timing { get; set; } = ReminderTiming.OneDay;
    public DateTime ScheduledAt { get; set; }      // When to send
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? FailureReason { get; set; }

    // Navigation
    public Booking? Booking { get; set; }
    public Client? Client { get; set; }
}

public enum ReminderType
{
    Email,
    SMS,
    Push,
    WhatsApp
}

public enum ReminderTiming
{
    FifteenMin,     // 15 minutes before
    OneHour,        // 1 hour before
    TwoHours,       // 2 hours before
    OneDay,         // 24 hours before
    TwoDays,        // 48 hours before
    OneWeek         // 7 days before
}
