namespace Upkilo.Core.Entities;

/// <summary>
/// User entity - staff/admin users belonging to a tenant
/// </summary>
public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? PhoneNumber { get; set; } // Compatibility alias
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.Staff;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool EmailVerified { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public Dictionary<string, object> Preferences { get; set; } = new();
    public Guid? CustomRoleId { get; set; }
    public string? SocialProvider { get; set; } // Google, Apple, etc.
    public string TimeZoneId { get; set; } = "UTC";
    public string LanguageCode { get; set; } = "en";

    // CCPA §1798.120 — Do Not Sell opt-out flag
    public bool DoNotSell { get; set; }
    public DateTime? DoNotSellUpdatedAt { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual StaffMember? StaffMember { get; set; }
    public virtual CustomRole? CustomRole { get; set; }
}

public enum UserRole
{
    Owner,
    Admin,
    Manager,
    Staff,
    SuperAdmin
}

public enum UserStatus
{
    Active,
    Inactive,
    Pending
}
