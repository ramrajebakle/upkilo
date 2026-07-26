using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks webhook delivery status, retries, and payloads for debugging.
/// </summary>
public class WebhookDeliveryLog : TenantEntity
{
    public string WebhookType { get; set; } = string.Empty; // stripe, sendgrid, twilio
    public string EventType { get; set; } = string.Empty; // e.g. payment_intent.succeeded
    public string? ExternalEventId { get; set; }
    public string Payload { get; set; } = string.Empty; // JSON
    public string Status { get; set; } = "Received"; // Received, Processing, Processed, Failed
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
