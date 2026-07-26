namespace Upkilo.Core.Entities;

/// <summary>
/// User activity log - tracks important user actions for audit purposes
/// </summary>
public class UserActivityLog : TenantEntity
{
    public Guid UserId { get; set; }
    public UserActivityType ActivityType { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ResourceType { get; set; } // e.g., "Client", "Booking", "Campaign"
    public Guid? ResourceId { get; set; }
    public string? Metadata { get; set; } // JSON for additional context

    // Navigation
    public virtual User? User { get; set; }
}

public enum UserActivityType
{
    Login,
    Logout,
    PasswordChange,
    ProfileUpdate,
    ClientCreate,
    ClientUpdate,
    ClientDelete,
    BookingCreate,
    BookingUpdate,
    BookingCancel,
    PaymentProcess,
    CampaignSend,
    ReportExport,
    SettingsChange,
    RoleChange,
    Other
}
