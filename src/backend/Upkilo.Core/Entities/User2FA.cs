namespace Upkilo.Core.Entities;

/// <summary>
/// Two-Factor Authentication settings for a user
/// </summary>
public class User2FA
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsEnabled { get; set; }
    public string? TotpSecret { get; set; } // Encrypted
    public string? BackupCodes { get; set; } // JSON array of hashed codes
    public int BackupCodesRemaining { get; set; } = 10;
    public string PreferredMethod { get; set; } = "totp"; // totp, sms, email
    public string? PhoneNumber { get; set; } // For SMS 2FA
    public string? SmsCode { get; set; } // Hashed SMS code
    public DateTime? SmsCodeExpiresAt { get; set; }
    public string? EmailCode { get; set; } // Hashed Email code
    public DateTime? EmailCodeExpiresAt { get; set; }
    public DateTime? EnabledAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User? User { get; set; }
}
