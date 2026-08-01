using Upkilo.Core.DTOs;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Authentication service interface for user authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Initiates password reset flow by generating a token and sending email
    /// </summary>
    Task<bool> InitiatePasswordResetAsync(string email);

    /// <summary>
    /// Resets password using the provided token
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword, Guid? tenantId = null);

    /// <summary>
    /// Sends email verification link to user
    /// </summary>
    Task<bool> SendEmailVerificationAsync(Guid userId);

    /// <summary>
    /// Verifies email using the provided token
    /// </summary>
    Task<(bool Success, string Message)> VerifyEmailAsync(string token, Guid? tenantId = null);

    /// <summary>
    /// Validates password strength
    /// </summary>
    (bool IsValid, string[] Errors) ValidatePasswordStrength(string password);

    /// <summary>
    /// Checks if password was previously used
    /// </summary>
    Task<bool> IsPasswordPreviouslyUsedAsync(Guid userId, string password);

    /// <summary>
    /// Records failed login attempt for brute force protection
    /// </summary>
    Task RecordFailedLoginAsync(string email, string ipAddress);

    /// <summary>
    /// Checks if account is locked due to too many failed attempts
    /// </summary>
    Task<(bool IsLocked, DateTime? UnlockTime)> IsAccountLockedAsync(string email);

    /// <summary>
    /// Authenticates a user and handles 2FA requirements
    /// </summary>
    Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string userAgent, string? deviceToken = null);

    /// <summary>
    /// Verifies 2FA code (TOTP or Backup)
    /// </summary>
    Task<AuthResult> VerifyTwoFactorAsync(string email, string code, bool isBackupCode, bool rememberDevice = false, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Registers a new user and creates their tenant
    /// </summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request, string ipAddress, string userAgent);

    /// <summary>
    /// Refreshes access token using a refresh token
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);

    /// <summary>
    /// Revokes a refresh token (logout)
    /// </summary>
    Task<bool> RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Gets full user profile for the current authenticated user
    /// </summary>
    Task<dynamic?> GetCurrentUserAsync(Guid userId);

    /// <summary>
    /// Sends a 2FA code via SMS to the user's registered phone number
    /// </summary>
    Task<AuthResponse> SendTwoFactorSmsAsync(string email);

    /// <summary>
    /// Sends a 2FA code via email to the user's registered email address
    /// </summary>
    Task<AuthResponse> SendTwoFactorEmailAsync(string email);

    /// <summary>
    /// Processes 2FA state changes (enabled/disabled) and sends notification emails
    /// </summary>
    Task ProcessTwoFactorStateChangeAsync(Guid userId, bool enabled);

    /// <summary>
    /// Submits a request to recover 2FA access when device is lost
    /// </summary>
    Task<AuthResponse> SubmitTwoFactorRecoveryRequestAsync(string email, string identityData);

    /// <summary>
    /// Approves or rejects a 2FA recovery request
    /// </summary>
    Task<bool> ProcessTwoFactorRecoveryRequestAsync(Guid requestId, Guid adminId, bool approve, string notes);

    /// <summary>
    /// Revokes all active sessions for a user (global sign-out)
    /// </summary>
    Task<bool> RevokeAllSessionsAsync(Guid userId);

    /// <summary>
    /// Authenticates via social login (Google/Apple), creates account if new
    /// </summary>
    Task<AuthResult> SocialLoginAsync(string email, string firstName, string lastName, string provider, string? avatarUrl, string ipAddress, string userAgent);

    Task<AuthResult> LoginWithBiometricAsync(Guid userId, string ipAddress, string userAgent);

    /// <summary>
    /// Authenticates via Enterprise SSO / SAML 2.0, auto-provisions user if configured
    /// </summary>
    Task<AuthResult> SsoLoginAsync(string email, string firstName, string lastName, string provider, Guid tenantId, string ipAddress, string userAgent);
}
