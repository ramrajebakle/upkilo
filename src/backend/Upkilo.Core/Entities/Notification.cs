using Upkilo.Core.Entities;

namespace Upkilo.Core.Entities;

/// <summary>
/// Notification entity for in-app notifications
/// </summary>
public class Notification : TenantEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Notification type (booking_confirmed, reminder, payment, system, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message body
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional link/action URL
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Related entity type (booking, payment, etc.)
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Has the notification been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When was it read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Priority level (low, normal, high, urgent)
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}

public static class NotificationType
{
    public const string System = "System";
    public const string Booking = "Booking";
    public const string Reminder = "Reminder";
    public const string Payment = "Payment";
    public const string Email = "Email";
    public const string Sms = "Sms";
}

/// <summary>
/// Webhook registration for external integrations
/// </summary>
public class Webhook : TenantEntity
{
    /// <summary>
    /// Webhook name/label
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target URL to send events to
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Secret for HMAC signature verification
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// List of event types to subscribe to
    /// </summary>
    public List<string> Events { get; set; } = new();

    /// <summary>
    /// Is the webhook active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Last successful delivery
    /// </summary>
    public DateTime? LastSuccessAt { get; set; }

    /// <summary>
    /// Last failure time
    /// </summary>
    public DateTime? LastFailureAt { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When was the webhook last triggered
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }
}

/// <summary>
/// Webhook delivery log
/// </summary>
public class WebhookDelivery : TenantEntity
{
    public Guid WebhookId { get; set; }
    public Webhook? Webhook { get; set; }

    /// <summary>
    /// Event type that triggered this delivery
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Request payload (JSON)
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// HTTP response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Response body (truncated)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Delivery attempt number
    /// </summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>
    /// Was delivery successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Scheduled reminder for bookings
/// </summary>
public class Reminder : TenantEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>
    /// Channel: email, sms, push
    /// </summary>
    public string Channel { get; set; } = "email";

    /// <summary>
    /// When to send the reminder
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Has it been sent
    /// </summary>
    public bool IsSent { get; set; }

    /// <summary>
    /// When was it sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Send result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Error if failed
    /// </summary>
    public string? Error { get; set; }
}
