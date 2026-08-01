namespace Upkilo.Core.Entities;

/// <summary>
/// User session for tracking active logins
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string? DeviceType { get; set; } // desktop, mobile, tablet
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; } // City, Country
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    // Navigation
    public User? User { get; set; }
}
