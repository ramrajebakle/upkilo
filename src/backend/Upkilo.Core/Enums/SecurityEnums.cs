namespace Upkilo.Core.Enums;

public enum SecurityEventType
{
    LoginSuccess,
    LoginFailed,
    PasswordChanged,
    TwoFactorEnabled,
    TwoFactorDisabled,
    ConsentGranted,
    ConsentRevoked,
    DataExported,
    AccountDeleted,
    SecurityAlert,
    PrivacyConsentGranted,
    PrivacyConsentWithdrawn,
    AuditLogViewed
}
