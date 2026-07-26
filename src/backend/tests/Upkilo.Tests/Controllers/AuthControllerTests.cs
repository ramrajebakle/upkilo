using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Upkilo.API.Controllers;
using Upkilo.Core.DTOs;
using Upkilo.Core.Interfaces;
using Upkilo.Tests.Helpers;
using MockFactory = Upkilo.Tests.Helpers.MockFactory;

namespace Upkilo.Tests.Controllers;

/// <summary>
/// AuthController tests — login, register, logout, refresh, password reset, social login, 2FA, email verification.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        var config = MockFactory.CreateConfiguration();
        _authService = new Mock<IAuthService>();
        var loginValidator = new Mock<IValidator<LoginRequest>>();
        // AuthController.Login awaits ValidateAsync directly. An unconfigured mock returns a null
        // Task, so awaiting it threw NullReferenceException before the controller logic ran.
        // These tests exercise login outcomes, not request validation — so always report valid.
        loginValidator
            .Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        var logger = MockFactory.CreateLogger<AuthController>();
        _sut = new AuthController(config, _authService.Object, loginValidator.Object, logger.Object);

        // Set up controller context with remote IP
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = System.Net.IPAddress.Loopback }
            }
        };
    }

    // ── Login ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        _authService.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new AuthResult { Success = true, Token = "jwt-token", RefreshToken = "refresh-token" });

        var result = await _sut.Login(new LoginRequest("user@test.com", "password"));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _authService.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new AuthResult { Success = false, Message = "Invalid credentials" });

        var result = await _sut.Login(new LoginRequest("user@test.com", "wrong"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_TwoFactorRequired_ReturnsOkWithFlag()
    {
        _authService.Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new AuthResult { Success = true, TwoFactorRequired = true });

        var result = await _sut.Login(new LoginRequest("user@test.com", "password"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    // ── Register ──────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns200()
    {
        _authService.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AuthResult { Success = true, Token = "jwt-token", RefreshToken = "refresh-token" });

        var result = await _sut.Register(new RegisterRequest("user@test.com", "StrongP@ss1!", "John", "Doe", "Acme"));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_ValidationFail_Returns400()
    {
        _authService.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AuthResult { Success = false, Message = "Email already exists" });

        var result = await _sut.Register(new RegisterRequest("existing@test.com", "StrongP@ss1!", "J", "D", null));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetCurrentUser ────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUser_Authenticated_Returns200()
    {
        var userId = Guid.NewGuid();
        _authService.Setup(a => a.GetCurrentUserAsync(userId))
            .ReturnsAsync(new { Id = userId, Email = "user@test.com" });

        // Set up authenticated context
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        };
        _sut.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));

        var result = await _sut.GetCurrentUser();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_InvalidUserId_Returns401()
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, "not-a-guid")
        };
        _sut.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));

        var result = await _sut.GetCurrentUser();
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        _authService.Setup(a => a.GetCurrentUserAsync(userId)).ReturnsAsync((object?)null);

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        };
        _sut.ControllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));

        var result = await _sut.GetCurrentUser();
        result.Should().BeOfType<NotFoundResult>();
    }

    // ── Logout ────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidRefreshToken_Returns200()
    {
        _authService.Setup(a => a.RevokeTokenAsync(It.IsAny<string>())).ReturnsAsync(true);

        var result = await _sut.Logout(new LogoutRequest("valid-refresh-token"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Logout_EmptyRefreshToken_Returns400()
    {
        var result = await _sut.Logout(new LogoutRequest(""));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── RefreshToken ──────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewPair()
    {
        _authService.Setup(a => a.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AuthResult { Success = true, Token = "new-jwt", RefreshToken = "new-refresh" });

        var result = await _sut.RefreshToken(new RefreshTokenRequest("valid-token"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        _authService.Setup(a => a.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AuthResult { Success = false, Message = "Token expired" });

        var result = await _sut.RefreshToken(new RefreshTokenRequest("expired-token"));
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_EmptyToken_Returns400()
    {
        var result = await _sut.RefreshToken(new RefreshTokenRequest(""));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── ForgotPassword ────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_AnyEmail_Returns200_PreventsEnumeration()
    {
        _authService.Setup(a => a.InitiatePasswordResetAsync(It.IsAny<string>())).ReturnsAsync(true);

        var result = await _sut.ForgotPassword(new ForgotPasswordRequest("nonexistent@test.com"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_EmptyEmail_Returns400()
    {
        var result = await _sut.ForgotPassword(new ForgotPasswordRequest(""));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── ResetPassword ─────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ValidToken_Returns200()
    {
        _authService.Setup(a => a.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync((true, "Password reset"));

        var result = await _sut.ResetPassword(new ResetPasswordRequest("valid-token", "NewP@ss123!"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        _authService.Setup(a => a.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync((false, "Token expired"));

        var result = await _sut.ResetPassword(new ResetPasswordRequest("expired-token", "NewP@ss123!"));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_MissingToken_Returns400()
    {
        var result = await _sut.ResetPassword(new ResetPasswordRequest("", "NewP@ss123!"));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Verify2fa ─────────────────────────────────────────────────────

    [Fact]
    public async Task Verify2fa_ValidCode_Returns200()
    {
        _authService.Setup(a => a.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<string>(), false, false, null, null))
            .ReturnsAsync(new AuthResult { Success = true, Token = "jwt", RefreshToken = "refresh" });

        var result = await _sut.Verify2fa(new TwoFactorLoginRequest("user@test.com", "123456"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Verify2fa_InvalidCode_Returns401()
    {
        _authService.Setup(a => a.VerifyTwoFactorAsync(It.IsAny<string>(), It.IsAny<string>(), false, false, null, null))
            .ReturnsAsync(new AuthResult { Success = false, Message = "Invalid code" });

        var result = await _sut.Verify2fa(new TwoFactorLoginRequest("user@test.com", "000000"));
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ── SendTwoFactorSms ──────────────────────────────────────────────

    [Fact]
    public async Task SendTwoFactorSms_ValidEmail_Returns200()
    {
        _authService.Setup(a => a.SendTwoFactorSmsAsync(It.IsAny<string>()))
            .ReturnsAsync(new AuthResponse(true, "Code sent"));

        var result = await _sut.SendTwoFactorSms(new SendSms2FaRequest("user@test.com"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendTwoFactorSms_EmptyEmail_Returns400()
    {
        var result = await _sut.SendTwoFactorSms(new SendSms2FaRequest(""));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── SendTwoFactorEmail ────────────────────────────────────────────

    [Fact]
    public async Task SendTwoFactorEmail_ValidEmail_Returns200()
    {
        _authService.Setup(a => a.SendTwoFactorEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AuthResponse(true, "Code sent"));

        var result = await _sut.SendTwoFactorEmail(new SendEmail2FaRequest("user@test.com"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendTwoFactorEmail_EmptyEmail_Returns400()
    {
        var result = await _sut.SendTwoFactorEmail(new SendEmail2FaRequest(""));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Request2faRecovery ────────────────────────────────────────────

    [Fact]
    public async Task RequestTwoFactorRecovery_ValidData_Returns200()
    {
        _authService.Setup(a => a.SubmitTwoFactorRecoveryRequestAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AuthResponse(true, "Request submitted"));

        var result = await _sut.RequestTwoFactorRecovery(new Request2FaRecoveryDto("user@test.com", "passport-data"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RequestTwoFactorRecovery_MissingData_Returns400()
    {
        var result = await _sut.RequestTwoFactorRecovery(new Request2FaRecoveryDto("", ""));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── VerifyEmail ───────────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_ValidToken_Returns200()
    {
        _authService.Setup(a => a.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync((true, "Verified"));

        var result = await _sut.VerifyEmail(new VerifyEmailRequest { Token = "valid-token", TenantId = Guid.NewGuid() });
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400()
    {
        _authService.Setup(a => a.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync((false, "Token expired"));

        var result = await _sut.VerifyEmail(new VerifyEmailRequest { Token = "expired-token", TenantId = Guid.NewGuid() });
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
