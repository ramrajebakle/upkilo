using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using FluentValidation;
using Upkilo.Core.Interfaces;
using Upkilo.Core.DTOs;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

/// <summary>
/// Authentication controller
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    // VULN-009 FIX: Shared static HttpClient instances replace per-request `new HttpClient()`.
    // Creating and disposing HttpClient per request exhausts OS TCP ports (TIME_WAIT state)
    // under concurrent load.  Static instances are safe because these clients only call
    // fixed external endpoints with no per-request configuration changes.
    private static readonly HttpClient _appleHttpClient = new HttpClient();
    private static readonly HttpClient _googleHttpClient = new HttpClient();
    private static JsonWebKeySet? _appleKeys;
    private static DateTime _appleKeysFetchedAt = DateTime.MinValue;
    // H-10 FIX: Lock for thread-safe Apple key cache refresh
    private static readonly SemaphoreSlim _appleKeysLock = new(1, 1);

    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfiguration configuration,
        IAuthService authService,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _authService = authService;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <response code="200">Returns the auth token or 2FA requirement</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Server-side validation regardless of client-side checks.
        // Returns a generic message — do NOT expose which field failed.
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Login validation failed from IP {IpAddress}: {Errors}",
                ipAddress, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(new { message = "Invalid request." });
        }

        var userAgent = Request.Headers["User-Agent"].ToString();
        var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress, userAgent);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        if (result.TwoFactorRequired)
        {
            return Ok(new 
            { 
                twoFactorRequired = true, 
                email = request.Email,
                message = "Two-factor authentication is required" 
            });
        }

        // Access token is also set as an HttpOnly cookie for SSR-proxy / same-origin calls.
        // The token + refreshToken are additionally returned in the body: they are consumed
        // ONLY by NextAuth's server-side authorize() callback, which stores them inside the
        // encrypted NextAuth session JWT. They never reach browser JavaScript through this
        // flow — the browser receives only a short-lived access token mirrored from the
        // session, and the refresh token stays server-side.
        SetAuthCookie(result.Token!);
        SetRefreshCookie(result.RefreshToken!);

        return Ok(new
        {
            token = result.Token,
            refreshToken = result.RefreshToken,
            user = result.User
        });
    }

    /// <response code="200">Successfully verified and logged in</response>
    /// <response code="401">Invalid 2FA code</response>
    [HttpPost("verify-2fa")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Verify2fa([FromBody] TwoFactorLoginRequest request)
    {
        var result = await _authService.VerifyTwoFactorAsync(request.Email, request.Code, request.IsBackupCode);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        SetAuthCookie(result.Token!);
        SetRefreshCookie(result.RefreshToken!);

        return Ok(new
        {
            token = result.Token,
            refreshToken = result.RefreshToken,
            user = result.User
        });
    }

    /// <response code="200">Successfully registered</response>
    /// <response code="400">Validation error or registration failed</response>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();
        var result = await _authService.RegisterAsync(request, ipAddress, userAgent);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (result.Token != null)
        {
            SetAuthCookie(result.Token);
            if (result.RefreshToken != null) SetRefreshCookie(result.RefreshToken);
        }

        return Ok(new
        {
            token = result.Token,
            refreshToken = result.RefreshToken,
            user = result.User,
            message = result.Message
        });
    }

    /// <summary>
    /// Get current user
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        // Get full user profile via interface
        var user = await _authService.GetCurrentUserAsync(userId);
        
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        // Clear both HttpOnly cookies on logout.
        Response.Cookies.Delete("token", new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Path     = "/",
            Domain   = IsDevelopment() ? null : ".upkilo.com"
        });
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Path     = "/api/v1/auth/refresh",
            Domain   = IsDevelopment() ? null : ".upkilo.com"
        });
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Refresh token — accepts token from httpOnly cookie (web) or request body (mobile).
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Web clients send the refresh_token as an httpOnly cookie (body is {}).
        // Mobile clients (Expo) send it in the request body via SecureStore.
        var refreshToken = string.IsNullOrEmpty(request?.RefreshToken)
            ? Request.Cookies["refresh_token"]
            : request.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, userAgent);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        // Rotate both cookies on refresh (prevents token replay attacks).
        SetAuthCookie(result.Token!);
        SetRefreshCookie(result.RefreshToken!);

        return Ok(new
        {
            token = result.Token,
            refreshToken = result.RefreshToken,
            user = result.User
        });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest(new { message = "Email is required" });

        await _authService.InitiatePasswordResetAsync(request.Email);
        
        // Always return success to prevent email enumeration
        return Ok(new { message = "If an account exists with this email, you will receive a password reset link." });
    }

    /// <summary>
    /// Reset password using token
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
            return BadRequest(new { message = "Token and new password are required" });

        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, request.TenantId);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = "Password has been reset successfully. Please login with your new password." });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        // C-07 FIX: Null check BEFORE accessing request.Token to prevent NullReferenceException
        if (request == null || string.IsNullOrEmpty(request.Token))
        {
            _logger.LogWarning("Verification failed: Token is missing from request body. Request object null: {IsNull}", request == null);
            return BadRequest(new { message = "Verification token is required" });
        }

        _logger.LogDebug("Verification attempt received for token: {TokenPrefix}", request.Token[..Math.Min(5, request.Token.Length)] + "...");

        var result = await _authService.VerifyEmailAsync(request.Token, request.TenantId);
        
        if (!result.Success)
        {
            _logger.LogWarning("Verification failed for token {Token}: {Message}", request.Token.Substring(0, Math.Min(5, request.Token.Length)) + "...", result.Message);
            return BadRequest(new { message = result.Message });
        }

        _logger.LogInformation("Email verified successfully for token {Token}", request.Token.Substring(0, Math.Min(5, request.Token.Length)) + "...");
        return Ok(new { message = "Email has been verified successfully." });
    }

    /// <summary>
    /// Resend verification email
    /// </summary>
    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        await _authService.SendEmailVerificationAsync(userId);
        
        return Ok(new { message = "Verification email has been sent." });
    }

    /// <summary>
    /// Send 2FA code via SMS
    /// </summary>
    [HttpPost("send-2fa-sms")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendTwoFactorSms([FromBody] SendSms2FaRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest(new { message = "Email is required" });

        var result = await _authService.SendTwoFactorSmsAsync(request.Email);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Send 2FA code via Email
    /// </summary>
    [HttpPost("send-2fa-email")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendTwoFactorEmail([FromBody] SendEmail2FaRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest(new { message = "Email is required" });

        var result = await _authService.SendTwoFactorEmailAsync(request.Email);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Request 2FA recovery
    /// </summary>
    [HttpPost("request-2fa-recovery")]
    public async Task<IActionResult> RequestTwoFactorRecovery([FromBody] Request2FaRecoveryDto request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.IdentityVerificationData))
            return BadRequest(new { message = "Email and verification data are required" });

        var result = await _authService.SubmitTwoFactorRecoveryRequestAsync(request.Email, request.IdentityVerificationData);
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // ─── Social Login ─────────────────────────────────────────────────────────

    /// <summary>
    /// Login or register with Google OAuth2 id_token
    /// </summary>
    [HttpPost("social/google")]
    public async Task<IActionResult> GoogleLogin([FromBody] SocialLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { message = "Google id_token is required." });

        try
        {
            // C-04 FIX: Validate the Google id_token using Google's tokeninfo endpoint
            // with mandatory audience (aud) validation to prevent token confusion attacks.
            // VULN-009 FIX: Uses the shared static _googleHttpClient instead of new HttpClient()
            // per request to prevent TCP port exhaustion under concurrent load.
            var googleResponse = await _googleHttpClient.GetStringAsync(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(request.IdToken)}");
            var payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(googleResponse);

            if (payload == null || !payload.ContainsKey("email"))
                return Unauthorized(new { message = "Invalid Google token." });

            // C-04 FIX: Validate audience claim to ensure the token was issued for THIS application
            var googleClientId = _configuration["GoogleAuth:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                _logger.LogError("SECURITY: GoogleAuth:ClientId is not configured. Google login is disabled.");
                return StatusCode(503, new { message = "Google authentication is not configured." });
            }
            if (!payload.ContainsKey("aud") || payload["aud"]?.ToString() != googleClientId)
            {
                _logger.LogWarning("SECURITY: Google token audience mismatch. Expected={Expected}, Got={Got}",
                    googleClientId, payload.ContainsKey("aud") ? payload["aud"] : "MISSING");
                return Unauthorized(new { message = "Invalid Google token — audience mismatch." });
            }

            var email = payload["email"].ToString()!;
            var firstName = payload.ContainsKey("given_name") ? payload["given_name"].ToString() : "";
            var lastName = payload.ContainsKey("family_name") ? payload["family_name"].ToString() : "";
            var avatarUrl = payload.ContainsKey("picture") ? payload["picture"].ToString() : null;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.SocialLoginAsync(
                email, firstName ?? "", lastName ?? "", "Google", avatarUrl, ipAddress, userAgent);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            // CRITICAL-03: Set HttpOnly cookie for social login
            SetAuthCookie(result.Token!);

            return Ok(new
            {
                refreshToken = result.RefreshToken,
                user = result.User,
                isNewUser = result.IsNewUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google social login failed");
            return Unauthorized(new { message = "Google authentication failed." });
        }
    }

    /// <summary>
    /// Login or register with Apple Sign-In id_token
    /// </summary>
    [HttpPost("social/apple")]
    public async Task<IActionResult> AppleLogin([FromBody] SocialLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { message = "Apple id_token is required." });

        try
        {
            // Fetch Apple's public keys
            var keys = await GetApplePublicKeysAsync();

            // C-05 FIX: Always validate audience. If AppleAuth:ClientId is not configured,
            // reject the request rather than disabling audience validation (which allows
            // tokens from ANY Apple app to authenticate users on this platform).
            var appleClientId = _configuration["AppleAuth:ClientId"];
            if (string.IsNullOrEmpty(appleClientId))
            {
                _logger.LogError("SECURITY: AppleAuth:ClientId is not configured. Apple login is disabled.");
                return StatusCode(503, new { message = "Apple authentication is not configured." });
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudience = appleClientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(request.IdToken, validationParameters, out var validatedToken);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Invalid Apple token — no email claim." });

            // Apple only sends name on the first authorization
            var firstName = request.FirstName ?? "";
            var lastName = request.LastName ?? "";

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.SocialLoginAsync(
                email, firstName, lastName, "Apple", null, ipAddress, userAgent);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            // CRITICAL-03: Set HttpOnly cookie for Apple social login
            SetAuthCookie(result.Token!);

            return Ok(new
            {
                refreshToken = result.RefreshToken,
                user = result.User,
                isNewUser = result.IsNewUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple social login failed");
            return Unauthorized(new { message = "Apple authentication failed." });
        }
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// CRITICAL-03: Issues the JWT as an HttpOnly, Secure, SameSite=Strict cookie.
    /// The raw token is never returned to JavaScript — XSS cannot steal it.
    /// </summary>
    private void SetAuthCookie(string token)
    {
        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 30);

        Response.Cookies.Append("token", token, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,                    // HTTPS only — never sent over plain HTTP
            SameSite  = SameSiteMode.Strict,     // blocks CSRF
            Expires   = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
            Path      = "/",
            // Scope cookie to root domain in production so api.upkilo.com can also
            // read the cookie set by app.upkilo.com (e.g. during SSR proxy requests).
            // SECURITY (F-05): this shares the session cookie across ALL *.upkilo.com
            // subdomains. It is mitigated by HttpOnly (no JS read) + the TenantMiddleware
            // cross-tenant 403. INVARIANT: tenant-authored HTML/JS must NEVER be served from a
            // *.upkilo.com origin — host tenant sites on a separate sandbox domain so this
            // cookie is never exposed to attacker-controlled script.
            Domain    = IsDevelopment() ? null : ".upkilo.com"
        });
    }

    /// <summary>
    /// Issues the refresh token as an HttpOnly cookie restricted to the refresh endpoint path.
    /// Scoping to /api/v1/auth/refresh means the cookie is sent only on that request —
    /// never leaked to application API calls.
    /// </summary>
    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Strict,
            Expires   = DateTimeOffset.UtcNow.AddDays(30),
            Path      = "/api/v1/auth/refresh",
            Domain    = IsDevelopment() ? null : ".upkilo.com"
        });
    }

    /// <summary>
    /// Returns true when running in Development — used to relax cookie constraints for local dev.
    /// </summary>
    private bool IsDevelopment()
    {
        var env = _configuration["ASPNETCORE_ENVIRONMENT"]
               ?? _configuration["Environment"]
               ?? "Production";
        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }

    // H-10 FIX: Thread-safe Apple key cache with SemaphoreSlim
    private async Task<IEnumerable<SecurityKey>> GetApplePublicKeysAsync()
    {
        if (_appleKeys == null || (DateTime.UtcNow - _appleKeysFetchedAt).TotalHours > 24)
        {
            await _appleKeysLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_appleKeys == null || (DateTime.UtcNow - _appleKeysFetchedAt).TotalHours > 24)
                {
                    var json = await _appleHttpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
                    _appleKeys = JsonWebKeySet.Create(json);
                    _appleKeysFetchedAt = DateTime.UtcNow;
                }
            }
            finally
            {
                _appleKeysLock.Release();
            }
        }
        return _appleKeys.GetSigningKeys();
    }
}

public class SocialLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
