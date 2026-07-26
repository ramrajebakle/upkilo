using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class OutboxMessage : TenantEntity
{
    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string Payload { get; set; } = string.Empty;
    
    public bool IsProcessed { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public int RetryCount { get; set; }
    
    [MaxLength(500)]
    public string? Error { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    // SC5: DLQ fields
    public DateTime? NextRetryAt { get; set; }
    public bool IsDeadLetter { get; set; }
    public DateTime? DeadLetteredAt { get; set; }

    // Alias for backward compatibility if needed in some contexts
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Type { get => EventType; set => EventType = value; }
}
