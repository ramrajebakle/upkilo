namespace Upkilo.Core.DTOs;

public record AuthResponse(bool Success, string Message);

public class AuthResult
{
    public bool Success { get; set; }
    public bool TwoFactorRequired { get; set; }
    public bool TwoFactorEnforced { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? DeviceToken { get; set; }
    public string? Message { get; set; }
    public dynamic? User { get; set; }
    public bool IsNewUser { get; set; }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? CompanyName, Guid? PlanId = null);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword, Guid? TenantId = null);
public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}
public record TwoFactorLoginRequest(string Email, string Code, bool IsBackupCode = false);
public record SendSms2FaRequest(string Email);
public record SendEmail2FaRequest(string Email);

public record Request2FaRecoveryDto(string Email, string IdentityVerificationData);
public record Process2FaRecoveryDto(bool Approve, string? Notes);
