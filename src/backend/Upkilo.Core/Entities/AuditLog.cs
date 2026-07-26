using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// LEGACY STUB: This entity is no longer used in the active model.
/// It is maintained only to satisfy compilation of historical migration files.
/// All new audit logic should use AuditEntry.
/// </summary>
[Obsolete("Use AuditEntry instead.")]
public class AuditLog : TenantEntity
{
    public Guid? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
