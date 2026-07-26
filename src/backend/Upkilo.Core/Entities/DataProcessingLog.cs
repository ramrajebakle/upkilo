using System;

namespace Upkilo.Core.Entities;

public class DataProcessingLog : TenantEntity
{
    public Guid ClientId { get; set; }
    public string Action { get; set; } = string.Empty; // Export, Delete, Anonymize
    public string? Reason { get; set; }
    public Guid PerformedBy { get; set; }
    public string Details { get; set; } = string.Empty; // JSON summary
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Client? Client { get; set; }
}
