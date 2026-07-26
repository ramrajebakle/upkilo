namespace Upkilo.Core.Entities;

/// <summary>
/// Dead Letter Queue entity for storing failed background job payloads.
/// When a background job (outbox message, webhook, etc.) exhausts all retries,
/// its payload is moved here for manual investigation and potential replay.
/// </summary>
public class DeadLetterMessage : BaseEntity
{
    public string Source { get; set; } = string.Empty;       // "OutboxProcessor", "WebhookDelivery", "Hangfire"
    public string EventType { get; set; } = string.Empty;    // "booking.created", "payment.received"
    public string Payload { get; set; } = string.Empty;      // Original JSON payload
    public string Error { get; set; } = string.Empty;        // Last error message
    public string? StackTrace { get; set; }
    public int OriginalRetryCount { get; set; }
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    public int? RetryCount { get; set; }                      // Tracker for retries from DLQ
    public string? QueueName { get; set; }                   // Name of the queue for re-enqueueing
    public bool IsResolved { get; set; }                     // Manually marked as resolved
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}
