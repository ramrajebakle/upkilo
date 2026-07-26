namespace Upkilo.Core.Entities;

/// <summary>
/// Records every login attempt for a user.
/// Enables "Recent login activity" UI and suspicious activity detection.
/// Stored for 90 days (configurable per plan).
/// </summary>
public class LoginHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceType { get; set; }          // desktop, mobile, tablet
    public string? Location { get; set; }             // City, Country (GeoIP)
    public LoginResult Result { get; set; }
    public string? FailureReason { get; set; }        // "InvalidPassword", "AccountLocked", "2FA_Failed"
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuspicious { get; set; }            // Flagged by anomaly detection
    public string? SuspiciousReason { get; set; }     // "NewDevice", "NewLocation", "TorExit", "BruteForce"

    // Navigation
    public User? User { get; set; }
}

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountLocked,
    TwoFactorRequired,
    TwoFactorFailed,
    AccountDisabled,
    SessionLimitReached
}
