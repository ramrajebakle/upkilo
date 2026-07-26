using Upkilo.Core.Enums;

namespace Upkilo.Core.Entities;

/// <summary>
/// Security event for SIEM integration and compliance auditing.
/// Records all security-relevant actions across the platform.
/// Retained for 365 days per DataRetentionJob.
/// </summary>
public class SecurityEvent : BaseEntity
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;     // e.g., "LOGIN_FAILED", "2FA_ENABLED"
    public SecuritySeverity Severity { get; set; } = SecuritySeverity.Info;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; }
    public string? Details { get; set; }                       // JSON payload with event-specific data
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime Timestamp { get => CreatedAt; set => CreatedAt = value; } // Alias
}

public enum SecuritySeverity
{
    Info,           // Normal events (login, logout)
    Warning,        // Unusual events (failed login, unknown device)
    High,           // Concerning events (multiple failures, suspicious IP)
    Critical        // Immediate attention (brute force, account takeover)
}

/// <summary>
/// Security event types for consistent categorization.
/// </summary>
public static class SecurityEventTypes
{
    // Authentication
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string LoginLocked = "LOGIN_LOCKED";
    public const string LogoutManual = "LOGOUT_MANUAL";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";

    // 2FA
    public const string TwoFactorEnabled = "2FA_ENABLED";
    public const string TwoFactorDisabled = "2FA_DISABLED";
    public const string TwoFactorFailed = "2FA_FAILED";
    public const string BackupCodeUsed = "BACKUP_CODE_USED";

    // Session
    public const string SessionCreated = "SESSION_CREATED";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string AllSessionsRevoked = "ALL_SESSIONS_REVOKED";
    public const string UnknownDeviceLogin = "UNKNOWN_DEVICE_LOGIN";

    // API Keys
    public const string ApiKeyCreated = "API_KEY_CREATED";
    public const string ApiKeyRevoked = "API_KEY_REVOKED";
    public const string ApiKeyUsedAfterExpiry = "API_KEY_EXPIRED_USAGE";

    // Account
    public const string AccountDeletionRequested = "ACCOUNT_DELETION_REQUESTED";
    public const string DataExportRequested = "DATA_EXPORT_REQUESTED";
    public const string RoleChanged = "ROLE_CHANGED";
    public const string PermissionsChanged = "PERMISSIONS_CHANGED";

    // Suspicious
    public const string BruteForceDetected = "BRUTE_FORCE_DETECTED";
    public const string ImpossibleTravel = "IMPOSSIBLE_TRAVEL";
    public const string SuspiciousIpAccess = "SUSPICIOUS_IP_ACCESS";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // Privacy/Consent
    public const string PrivacyConsentGranted = "PRIVACY_CONSENT_GRANTED";
    public const string PrivacyConsentWithdrawn = "PRIVACY_CONSENT_WITHDRAWN";
}
