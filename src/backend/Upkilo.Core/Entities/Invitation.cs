using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// Team invitation entity for onboarding new staff members
/// </summary>
public class Invitation : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public Guid InvitedByUserId { get; set; }

    // Navigation
    public virtual User? InvitedBy { get; set; }
    public virtual Tenant? Tenant { get; set; }
}
