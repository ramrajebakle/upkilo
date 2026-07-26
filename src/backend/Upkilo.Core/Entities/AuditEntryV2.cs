using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// Enhanced audit entry for production with deep indexing.
/// Implements Task 1902/1903 (Audit Export & Retention).
/// </summary>
public class AuditEntryV2 : TenantEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, Logout
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Additional indexing for Phase 10
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public double? DurationMs { get; set; }
}
