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
using Microsoft.EntityFrameworkCore;


namespace Upkilo.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly AuthService _sut;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ITwoFactorService> _twoFactorService;
    private readonly Mock<IValidator<RegisterRequest>> _registerValidator;
    private readonly Mock<ILogger<AuthService>> _logger;

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

        _logger = new Mock<ILogger<AuthService>>();
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
            _logger.Object,
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

    /// <summary>
    /// Seeds the pricing catalogue signup resolves against. PricingSeeder does this at startup in
    /// production; the test DB starts empty.
    ///
    /// Growth matters here specifically: it is the default trial plan (Trial:PlanName), and if it
    /// is absent ResolveTrialPlanAsync falls back to Free — which silently turns a "did the trial
    /// grant Growth?" assertion into a "did it grant Free?" assertion that passes for the wrong
    /// reason.
    /// </summary>
    private async Task<(PricingPlan Free, PricingPlan Starter, PricingPlan Growth)> SeedPricingPlansAsync()
    {
        var context = _dbFactory.CreateContext();
        var free = new PricingPlan { Id = Guid.NewGuid(), Name = "Free", Description = "Free plan", IsActive = true, TrialDays = 14 };
        var starter = new PricingPlan { Id = Guid.NewGuid(), Name = "Starter", Description = "Starter plan", IsActive = true, TrialDays = 14 };
        var growth = new PricingPlan { Id = Guid.NewGuid(), Name = "Growth", Description = "Growth plan", IsActive = true, TrialDays = 14 };
        context.PricingPlans.AddRange(free, starter, growth);
        await context.SaveChangesAsync();
        return (free, starter, growth);
    }

    /// <summary>
    /// Tenant.SubscriptionTier and Tenant.PricingPlanId are the columns AiModelResolver,
    /// JobQuotaService and TenantRateLimitMiddleware actually gate on. Registration used to
    /// leave both at their entity defaults — SubscriptionTier defaults to Starter — so the tenant
    /// was gated against a plan nobody had chosen.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_SetsTenantPlanColumnsToFreeUntilVerified()
    {
        var plans = await SeedPricingPlansAsync();
        var request = new RegisterRequest("plancols@example.com", "StrongP@ss1!", "Plan", "Cols", "Plan Co", null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Name == "Plan Co");
        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Free);
        tenant.PricingPlanId.Should().Be(plans.Free.Id);
    }

    /// <summary>
    /// Pulls the one-time token out of the verification email the mock captured, which is the only
    /// place it exists in plaintext — the DB stores a hash.
    /// </summary>
    private string CapturedVerificationToken()
    {
        var body = _emailService.Invocations
            .Where(i => i.Method.Name == nameof(IEmailService.SendSecurityEmailAsync))
            .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
            .LastOrDefault(b => b.Contains("verify-email?token="));

        body.Should().NotBeNull("a verification email should have been sent");
        return System.Text.RegularExpressions.Regex.Match(body!, @"verify-email\?token=([^&""]+)").Groups[1].Value;
    }

    /// <summary>
    /// The reverse trial: every signup starts on the top plan for TrialDays, then lands on Free.
    /// All of this machinery pre-existed — TrialEndsAt, SubscriptionStatus.Trialing,
    /// PricingPlan.TrialDays, UpsellTriggerService's trial_ending trigger — and was completely
    /// inert because nothing ever set TrialEndsAt.
    /// </summary>
    /// <summary>
    /// The trial is the expensive grant — top plan plus an AI budget — so it is gated on a
    /// provably real address. Granting it at signup makes unlimited free Growth trials a matter of
    /// typing a different throwaway address.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_DoesNotStartTheTrialBeforeVerification()
    {
        var plans = await SeedPricingPlansAsync();
        var request = new RegisterRequest("trial@example.com", "StrongP@ss1!", "Tri", "Al", "Trial Co", null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var context = _dbFactory.CreateContext();
        var tenant = await context.Tenants.FirstAsync(t => t.Name == "Trial Co");
        var subscription = await context.Subscriptions.FirstAsync(s => s.TenantId == tenant.Id);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.PricingPlanId.Should().Be(plans.Free.Id);
        tenant.TrialEndsAt.Should().BeNull();
    }

    /// <summary>
    /// Verifying is what earns the trial — a carrot rather than a toll. Login is deliberately NOT
    /// gated, so an email that lands in spam never strands anyone.
    /// </summary>
    [Fact]
    public async Task VerifyEmailAsync_StartsTheTrialOnTheTopPlan()
    {
        var plans = await SeedPricingPlansAsync();
        var request = new RegisterRequest("verifytrial@example.com", "StrongP@ss1!", "Ver", "Ify", "Verify Co", null);
        (await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent")).Success.Should().BeTrue();

        var (ok, message) = await _sut.VerifyEmailAsync(CapturedVerificationToken());

        ok.Should().BeTrue(because: message);
        message.Should().Contain("trial has started");

        var context = _dbFactory.CreateContext();
        var tenant = await context.Tenants.FirstAsync(t => t.Name == "Verify Co");
        var subscription = await context.Subscriptions.FirstAsync(s => s.TenantId == tenant.Id);

        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.PricingPlanId.Should().Be(plans.Growth.Id);
        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Growth);
        tenant.TrialEndsAt.Should().NotBeNull();
        tenant.TrialEndsAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// TrialEndsAt survives expiry as the record that a trial happened, and it doubles as the
    /// guard stopping a second one being farmed by re-verifying.
    /// </summary>
    [Fact]
    public async Task VerifyEmailAsync_CannotStartASecondTrial()
    {
        await SeedPricingPlansAsync();
        var request = new RegisterRequest("twice@example.com", "StrongP@ss1!", "Twi", "Ce", "Twice Co", null);
        (await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent")).Success.Should().BeTrue();

        (await _sut.VerifyEmailAsync(CapturedVerificationToken())).Success.Should().BeTrue();

        var firstEnd = (await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Name == "Twice Co")).TrialEndsAt;
        firstEnd.Should().NotBeNull();

        // Issue and redeem a fresh token for the same user.
        var user = await _dbFactory.CreateContext().Users.FirstAsync(u => u.Email == "twice@example.com");
        await _sut.SendEmailVerificationAsync(user.Id);
        await _sut.VerifyEmailAsync(CapturedVerificationToken());

        var after = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Name == "Twice Co");
        after.TrialEndsAt.Should().Be(firstEnd);
    }

    /// <summary>
    /// BillingController carries a class-level [Authorize(Roles = "Owner")] and the Stripe Connect
    /// endpoints are Owner-only too. Signup assigned Admin, and no signup path anywhere ever
    /// created an Owner — so the founder of every self-service tenant could not open billing,
    /// could not connect Stripe, and could not create a checkout session to pay us.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_MakesTheFounderTheOwner()
    {
        await SeedPricingPlansAsync();
        var request = new RegisterRequest("founder@example.com", "StrongP@ss1!", "Found", "Er", "Founder Co", null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var user = await _dbFactory.CreateContext().Users.FirstAsync(u => u.Email == "founder@example.com");
        user.Role.Should().Be(UserRole.Owner);
    }

    /// <summary>
    /// OnboardingDripJob skips any tenant with no Tenant.Email, and CreateStripeCustomerAsync
    /// sends it to Stripe. Registration only ever set User.Email, so both were operating on null.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_SetsTenantEmail()
    {
        await SeedPricingPlansAsync();
        var request = new RegisterRequest("TenantMail@Example.com", "StrongP@ss1!", "Tenant", "Mail", "Mail Co", null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Name == "Mail Co");
        tenant.Email.Should().Be("tenantmail@example.com");
    }

    /// <summary>
    /// The progress row used to be created lazily by GET /onboarding/checklist, so its CreatedAt
    /// meant "first opened the dashboard" rather than "signed up" — and a tenant who never came
    /// back had no row at all, which is precisely who the 7-day drip is meant to reach.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_CreatesOnboardingProgressRow()
    {
        await SeedPricingPlansAsync();
        var request = new RegisterRequest("progress@example.com", "StrongP@ss1!", "Prog", "Ress", "Progress Co", null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var context = _dbFactory.CreateContext();
        var tenant = await context.Tenants.FirstAsync(t => t.Name == "Progress Co");
        var user = await context.Users.FirstAsync(u => u.Email == "progress@example.com");

        var progress = await context.Set<TenantOnboardingProgress>()
            .SingleAsync(p => p.TenantId == tenant.Id);
        progress.UserId.Should().Be(user.Id);
    }

    /// <summary>
    /// The slug is the public booking URL (/book/{slug}). It was built with
    /// companyName.ToLower().Replace(" ", "-"), which handles spaces and nothing else — so an
    /// ampersand, a dot or a slash went straight into the route.
    /// </summary>
    [Theory]
    [InlineData("Café & Co. / Ltd")]
    [InlineData("  Acme  ")]
    [InlineData("北京")]
    [InlineData("!!!")]
    public async Task RegisterAsync_ProducesUrlSafeSlug(string companyName)
    {
        await SeedPricingPlansAsync();
        var email = $"slug{Math.Abs(companyName.GetHashCode())}@example.com";
        var request = new RegisterRequest(email, "StrongP@ss1!", "Slug", "Test", companyName, null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var user = await _dbFactory.CreateContext().Users.FirstAsync(u => u.Email == email);
        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Id == user.TenantId);

        tenant.Slug.Should().MatchRegex("^[a-z0-9]+(-[a-z0-9]+)*$");
        tenant.Slug.Should().NotStartWith("-").And.NotEndWith("-");
    }

    /// <summary>
    /// `companyName ?? firstName` does not catch the empty string, so an API caller omitting a
    /// company name got a tenant literally named "" and a slug of just the random suffix.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_BlankCompanyName_FallsBackToAPersonalOrgName()
    {
        await SeedPricingPlansAsync();
        var request = new RegisterRequest("blankco@example.com", "StrongP@ss1!", "Jane", "Doe", "   ", null);

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var user = await _dbFactory.CreateContext().Users.FirstAsync(u => u.Email == "blankco@example.com");
        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Id == user.TenantId);

        tenant.Name.Should().NotBeNullOrWhiteSpace();
        tenant.Name.Should().Contain("Jane");
    }

    /// <summary>
    /// Marketing pricing pages link to /register?plan=starter — a plan NAME, which a Guid PlanId
    /// could never resolve, so the intent was discarded at the last step of the funnel.
    ///
    /// Under the reverse trial that name no longer selects the subscription — everyone trials the
    /// top plan — so it is recorded as intent, for the upgrade CTA to pre-select later.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_RecordsTheRequestedPlanAsIntentAndStillTrialsTheTopPlan()
    {
        var plans = await SeedPricingPlansAsync();
        var request = new RegisterRequest("byname@example.com", "StrongP@ss1!", "By", "Name", "Byname Co", null, "starter");

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Name == "Byname Co");

        tenant.PricingPlanId.Should().Be(plans.Free.Id);
        tenant.Metadata.Should().ContainKey("intended_plan");
        tenant.Metadata["intended_plan"].ToString().Should().Be("Starter");
    }

    /// <summary>
    /// An unrecognised plan name must not fail the signup, and must not be recorded as intent —
    /// the trial proceeds normally.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_UnknownPlanName_IsIgnoredAndTrialStillStarts()
    {
        var plans = await SeedPricingPlansAsync();
        var request = new RegisterRequest("badplan@example.com", "StrongP@ss1!", "Bad", "Plan", "Badplan Co", null, "enterprise-plus");

        var result = await _sut.RegisterAsync(request, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Name == "Badplan Co");
        tenant.PricingPlanId.Should().Be(plans.Free.Id);
        tenant.Metadata.Should().NotContainKey("intended_plan");
    }

    /// <summary>
    /// Subscription.AiMonthlyBudget defaults to 0 and the entity documents "&lt;=0 means no
    /// access". Password signup sets $5; social signup never did, so Google/Apple signups were
    /// locked out of AI from the first login.
    /// </summary>
    [Fact]
    public async Task SocialLoginAsync_NewUser_ProvisionsSameAsPasswordSignup()
    {
        var plans = await SeedPricingPlansAsync();

        var result = await _sut.SocialLoginAsync("parity@example.com", "Par", "Ity", "Google", null, "127.0.0.1", "TestAgent");
        result.Success.Should().BeTrue(because: result.Message);

        var context = _dbFactory.CreateContext();
        var user = await context.Users.FirstAsync(u => u.Email == "parity@example.com");
        var tenant = await context.Tenants.FirstAsync(t => t.Id == user.TenantId);
        var subscription = await context.Subscriptions.FirstAsync(s => s.TenantId == tenant.Id);

        subscription.AiMonthlyBudget.Should().BeGreaterThan(0m);
        // A Google signup and an email signup must get the same product, trial included.
        subscription.PricingPlanId.Should().Be(plans.Growth.Id);
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        tenant.Email.Should().Be("parity@example.com");
        tenant.PricingPlanId.Should().Be(plans.Growth.Id);
        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Growth);
        tenant.TrialEndsAt.Should().NotBeNull();
        tenant.Slug.Should().MatchRegex("^[a-z0-9]+(-[a-z0-9]+)*$");
        (await context.Set<TenantOnboardingProgress>().CountAsync(p => p.TenantId == tenant.Id))
            .Should().Be(1);
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
        // Arrange
        // Social signup now throws on a missing Free plan rather than writing a subscription with
        // a null PricingPlanId, matching RegisterAsync. PricingSeeder guarantees the plan in
        // production; seed it here as the other provisioning tests do.
        await SeedPricingPlansAsync();

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

    // ---- Token reuse detection ----
    //
    // Presenting a revoked refresh token revokes every remaining session for that user, on the
    // assumption the token was stolen. That response is correct and unchanged.
    //
    // What was wrong is that it repeated. Production logged 396 of these in one five-minute
    // burst — a client replaying a single revoked token — and each one reloaded the user's
    // sessions and called SaveChanges. After the first there was nothing left to revoke, so
    // every later write achieved nothing while still costing a query and a transaction, turning
    // an unauthenticated endpoint into cheap write amplification for anyone holding one dead
    // token. It also buried the one warning that mattered under 396 identical copies.

    /// <summary>Mirrors AuthService.HashToken, which stores the SHA-256 of the raw token.</summary>
    private static string HashTokenForTest(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token)));
    }

    /// <summary>Seeds a user with one revoked session plus <paramref name="activeCount"/> live ones.</summary>
    private async Task<Guid> SeedRevokedSessionAsync(string rawToken, int activeCount)
    {
        var db = _dbFactory.CreateContext();
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            FirstName = "Re",
            LastName = "Use",
            PasswordHash = "x",
            Role = UserRole.Owner,
        });

        db.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshToken = HashTokenForTest(rawToken),
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        });

        for (var i = 0; i < activeCount; i++)
        {
            db.UserSessions.Add(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RefreshToken = HashTokenForTest($"other-{userId}-{i}"),
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            });
        }

        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedTokenReuse_RevokesEveryRemainingSession()
    {
        // The security response itself must not change.
        var token = $"reused-{Guid.NewGuid()}";
        var userId = await SeedRevokedSessionAsync(token, activeCount: 3);

        var result = await _sut.RefreshTokenAsync(token, "127.0.0.1", "TestAgent");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("compromised");

        var stillActive = await _dbFactory.CreateContext().UserSessions
            .CountAsync(s => s.UserId == userId && !s.IsRevoked);
        stillActive.Should().Be(0, "a stolen token must lock the account down");
    }

    [Fact]
    public async Task RefreshTokenAsync_ReplayingTheSameDeadToken_StopsDoingTheWorkAgain()
    {
        var token = $"reused-{Guid.NewGuid()}";
        var userId = await SeedRevokedSessionAsync(token, activeCount: 2);

        // First hit locks the account down.
        await _sut.RefreshTokenAsync(token, "127.0.0.1", "TestAgent");

        // Nine replays, as the retry loop produced. Each must still be refused.
        for (var i = 0; i < 9; i++)
        {
            var replay = await _sut.RefreshTokenAsync(token, "127.0.0.1", "TestAgent");
            replay.Success.Should().BeFalse("a dead token is refused every time");
        }

        (await _dbFactory.CreateContext().UserSessions
            .CountAsync(s => s.UserId == userId && !s.IsRevoked))
            .Should().Be(0, "the account stays locked down across every replay");

        // The alarm fires once, for the hit that actually revoked something. Production logged
        // 396 identical warnings in five minutes, which buried the one that mattered.
        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("reuse detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "replays of a token already handled are not new compromises");
    }
}
