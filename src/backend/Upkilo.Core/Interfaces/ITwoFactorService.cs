namespace Upkilo.Core.Interfaces;

public interface ITwoFactorService
{
    Task<TwoFactorSetupResult> SetupTotpAsync(Guid userId);
    Task<bool> VerifyTotpAsync(Guid userId, string code);
    Task<bool> EnableTwoFactorAsync(Guid userId, string verificationCode);
    Task DisableTwoFactorAsync(Guid userId);
    Task<string[]> GenerateBackupCodesAsync(Guid userId);
    Task<bool> VerifyBackupCodeAsync(Guid userId, string code);
    Task<bool> IsTwoFactorEnabledAsync(Guid userId);
    Task ResetTwoFactorAsync(Guid userId);

    // Trusted device support (Task 18)
    Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceToken);
    Task<string> TrustDeviceAsync(Guid userId, string userAgent);

    // 2FA enforcement per role/tenant (Task 19)
    Task<bool> IsTwoFactorEnforcedAsync(Guid userId);

    Task<bool> InitiateSmsCodeAsync(Guid userId);
    Task<bool> VerifySmsCodeAsync(Guid userId, string code);
    Task<bool> InitiateEmailCodeAsync(Guid userId);
    Task<bool> VerifyEmailCodeAsync(Guid userId, string code);
}

public class TwoFactorSetupResult
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
}
