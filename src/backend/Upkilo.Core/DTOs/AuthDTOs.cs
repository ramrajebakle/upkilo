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
/// <summary>
/// PlanName exists because the marketing pricing pages link to /register?plan=starter — a plan
/// NAME, not an id. PlanId is a Guid and could never resolve one, so every plan-scoped signup
/// link silently landed on Free. PlanId still wins when both are supplied; the name is a
/// case-insensitive lookup that falls back to Free rather than failing the signup.
/// </summary>
/// <param name="Attribution">
/// Signup attribution (utm_source, utm_medium, utm_campaign, utm_content, utm_term, vertical,
/// referrer). The Powered-by-Upkilo widget builds a full UTM chain into its /register link and
/// the vertical landing pages tag theirs, but the register form read no query parameters at all,
/// so every one of those was discarded at the last step of the funnel. Stored on
/// Tenant.Metadata — which is already a jsonb bag — rather than as new columns, and filtered
/// server-side against a fixed key allowlist.
/// </param>
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? CompanyName,
    Guid? PlanId = null,
    string? PlanName = null,
    Dictionary<string, string>? Attribution = null);
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
