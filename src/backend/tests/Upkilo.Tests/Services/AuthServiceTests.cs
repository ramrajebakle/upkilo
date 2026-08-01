using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.Core.DTOs;
using Upkilo.Infrastructure.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;


namespace Upkilo.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly AuthService _sut;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ITwoFactorService> _twoFactorService;
    private readonly Mock<IValidator<RegisterRequest>> _registerValidator;

    public AuthServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        var context = _dbFactory.CreateContext();
        _emailService = new Mock<IEmailService>();
        _twoFactorService = new Mock<ITwoFactorService>();
        var secretProvider = new Mock<ISecretProvider>();
        secretProvider.Setup(s => s.GetSecret("Jwt:Secret")).Returns("ThisIsAVeryLongTestSecretKeyThatIsAtLeast32CharactersLong!");
        var subscriptionService = new Mock<ISubscriptionService>();
        _registerValidator = new Mock<IValidator<RegisterRequest>>();

        // Default valid validation result
        _registerValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsAVeryLongTestSecretKeyThatIsAtLeast32CharactersLong!",
                ["Jwt:Issuer"] = "Upkilo.Tests",
                ["Jwt:Audience"] = "Upkilo.Tests",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        var logger = new Mock<ILogger<AuthService>>();
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        var siemLogger = new Mock<SiemLoggingService>(new System.Net.Http.HttpClient(), new Mock<IConfiguration>().Object, new Mock<ILogger<SiemLoggingService>>().Object);

        var metrics = new Mock<IBusinessMetrics>();
        var connectionSelector = new Mock<IDbConnectionSelector>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        _sut = new AuthService(
            context,
            _emailService.Object,
            _twoFactorService.Object,
            config,
            secretProvider.Object,
            subscriptionService.Object,
            siemLogger.Object,
            cache.Object,
            metrics.Object,
            logger.Object,
            _registerValidator.Object,
            connectionSelector.Object,
            httpContextAccessor.Object);
    }

    public void Dispose() => _dbFactory.Dispose();

    // ---- Password Validation Tests ----

    [Fact]
    public void ValidatePasswordStrength_ValidPassword_ReturnsValid()
    {
        var result = _sut.ValidatePasswordStrength("StrongP@ss1");
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePasswordStrength_EmptyPassword_ReturnsErrors()
    {
        var result = _sut.ValidatePasswordStrength("");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Password is required");
    }

    [Fact]
    public void ValidatePasswordStrength_NoUppercase_ReturnsError()
    {
        var result = _sut.ValidatePasswordStrength("weakpass1!");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("uppercase"));
    }

    [Fact]
    public void ValidatePasswordStrength_NoLowercase_ReturnsError()
    {
        var result = _sut.ValidatePasswordStrength("ALLCAPS1!");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("lowercase"));
    }

    [Fact]
    public void ValidatePasswordStrength_NoNumber_ReturnsError()
    {
        var result = _sut.ValidatePasswordStrength("NoNumber!");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("number"));
    }

    [Fact]
    public void ValidatePasswordStrength_NoSpecialChar_ReturnsError()
    {
        var result = _sut.ValidatePasswordStrength("NoSpec1al");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("special"));
    }

    [Fact]
    public void ValidatePasswordStrength_TooShort_ReturnsError()
    {
        var result = _sut.ValidatePasswordStrength("Ab1!");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at least"));
    }

    [Fact]
    public void ValidatePasswordStrength_MultipleErrors_ReturnsAll()
    {
        var result = _sut.ValidatePasswordStrength("abc");
        result.IsValid.Should().BeFalse();
        result.Errors.Length.Should().BeGreaterOrEqualTo(3);
    }

    // ---- Login Tests ----

    [Fact]
    public async Task LoginAsync_NonexistentEmail_ReturnsFailure()
    {
        var result = await _sut.LoginAsync("nonexistent@example.com", "password", "127.0.0.1", "TestAgent");
        result.Success.Should().BeFalse();
        // Deliberately identical to the wrong-password message so the response cannot be
        // used to enumerate which email addresses exist.
        result.Message.Should().Be("Incorrect email or password");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        // Arrange — seed a user
        var context = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Wrong Password Tenant",
            Slug = "wrong-pwd-tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "wrongpwd@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectP@ss1"),
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await _sut.LoginAsync("wrongpwd@example.com", "WrongPassword1!", "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsSuccess()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test Co",
            Slug = "testco",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "valid@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectP@ss1"),
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await _sut.LoginAsync("valid@example.com", "CorrectP@ss1", "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
    }

    // ---- Password Reset Tests ----

    [Fact]
    public async Task InitiatePasswordResetAsync_NonexistentEmail_SucceedsSilently()
    {
        // Security: should not reveal whether email exists
        var result = await _sut.InitiatePasswordResetAsync("nobody@nowhere.com");
        result.Should().BeTrue();
    }

    // ---- Registration Tests ----

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        // RegisterAsync assigns every new tenant the "Free" plan and throws if it is absent
        // (AuthService logs Critical: "Run the seeder before registering users"). In production
        // PricingSeeder guarantees this at startup; the test DB starts empty, so seed it here.
        var seedContext = _dbFactory.CreateContext();
        seedContext.PricingPlans.Add(new PricingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Free",
            Description = "Free plan",
            IsActive = true
        });
        await seedContext.SaveChangesAsync();

        var request = new RegisterRequest("newuser@example.com", "StrongP@ss1!", "New", "User", "New Co", null);

        // Act
        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeTrue(because: result.Message);
        result.Token.Should().NotBeNullOrEmpty();

        var context = _dbFactory.CreateContext();
        context.Users.Should().Contain(u => u.Email == "newuser@example.com");
        context.Tenants.Should().Contain(t => t.Name == "New Co");
    }

    [Fact]
    public async Task RegisterAsync_ValidationFailure_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest("invalid", "", "", "", null, null);
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Invalid email format")
        };
        _registerValidator.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

        // Act
        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email format");
    }

    // ---- Social Login Tests ----

    [Fact]
    public async Task SocialLoginAsync_NewUser_CreatesTenantAndUserAndReturnsSuccess()
    {
        // Act
        var result = await _sut.SocialLoginAsync("social@example.com", "Social", "User", "Google", "http://avatar", "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeTrue();
        result.IsNewUser.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();

        var context = _dbFactory.CreateContext();
        context.Users.Should().Contain(u => u.Email == "social@example.com");
    }

    // ---- SSO Login Tests ----

    [Fact]
    public async Task SsoLoginAsync_UserNotFound_ReturnsFailure()
    {
        // Act
        var result = await _sut.SsoLoginAsync("sso@example.com", "SSO", "User", "Okta", Guid.NewGuid(), "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("does not exist");
    }

    // ---- Two Factor Tests ----

    [Fact]
    public async Task VerifyTwoFactorAsync_UserNotFound_ReturnsFailure()
    {
        // Act
        var result = await _sut.VerifyTwoFactorAsync("notfound@example.com", "123456", false);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid credentials");
    }

    // ---- Refresh Token Tests ----

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsFailure()
    {
        // Act
        var result = await _sut.RefreshTokenAsync("invalid-token", "127.0.0.1", "TestAgent");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid refresh token");
    }
}
