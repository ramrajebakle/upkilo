using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// Records admin impersonation sessions for audit and support purposes.
/// </summary>
public class AdminImpersonationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public Guid TargetTenantId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string? ActionsPerformed { get; set; } // JSON summary
    public string IpAddress { get; set; } = string.Empty;
}
