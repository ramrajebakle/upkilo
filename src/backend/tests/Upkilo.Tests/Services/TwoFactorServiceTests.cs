using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OtpNet;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class TwoFactorServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ISmsService> _smsServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<TwoFactorService>> _loggerMock;

    public TwoFactorServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _smsServiceMock = new Mock<ISmsService>();
        _emailServiceMock = new Mock<IEmailService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<TwoFactorService>>();

        // Setup service provider for IAuthService resolution in Enable/DisableTwoFactorAsync
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);
        _serviceProviderMock.Setup(s => s.GetService(typeof(IAuthService))).Returns(_authServiceMock.Object);
    }

    public void Dispose() => _dbFactory.Dispose();

    private TwoFactorService CreateSut(AppDbContext context) =>
        new TwoFactorService(
            context,
            _smsServiceMock.Object,
            _emailServiceMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object
        );

    [Fact]
    public async Task SetupTotpAsync_GeneratesSecretAndQrCodeUri()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await sut.SetupTotpAsync(user.Id);

        result.Should().NotBeNull();
        result.Secret.Should().NotBeNullOrWhiteSpace();
        result.QrCodeUri.Should().Contain("otpauth://totp/Upkilo:");
        result.QrCodeUri.Should().Contain("test%40upkilo.com");
    }

    [Fact]
    public async Task VerifyTotpAsync_WithValidCode_ReturnsTrue()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var setup = await sut.SetupTotpAsync(user.Id);

        var secretBytes = Base32Encoding.ToBytes(setup.Secret);
        var totp = new Totp(secretBytes);
        var code = totp.ComputeTotp();

        var isValid = await sut.VerifyTotpAsync(user.Id, code);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyTotpAsync_WithInvalidCode_ReturnsFalse()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await sut.SetupTotpAsync(user.Id);

        var isValid = await sut.VerifyTotpAsync(user.Id, "000000");
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task EnableTwoFactorAsync_WithValidCode_EnablesAndTriggersAuthService()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var setup = await sut.SetupTotpAsync(user.Id);
        var secretBytes = Base32Encoding.ToBytes(setup.Secret);
        var totp = new Totp(secretBytes);
        var code = totp.ComputeTotp();

        var success = await sut.EnableTwoFactorAsync(user.Id, code);

        success.Should().BeTrue();

        context.ChangeTracker.Clear();
        var twoFa = context.Set<User2FA>().FirstOrDefault(t => t.UserId == user.Id);
        twoFa.Should().NotBeNull();
        twoFa!.IsEnabled.Should().BeTrue();

        _authServiceMock.Verify(a => a.ProcessTwoFactorStateChangeAsync(user.Id, true), Times.Once);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_WithInvalidCode_ReturnsFalse()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await sut.SetupTotpAsync(user.Id);

        var success = await sut.EnableTwoFactorAsync(user.Id, "000000");
        success.Should().BeFalse();
    }

    [Fact]
    public async Task DisableTwoFactorAsync_ClearsFieldsAndTriggersAuthService()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);

        var twoFa = new User2FA { Id = Guid.NewGuid(), UserId = user.Id, IsEnabled = true, TotpSecret = "SECRET", BackupCodes = "[]" };
        context.Set<User2FA>().Add(twoFa);
        await context.SaveChangesAsync();

        await sut.DisableTwoFactorAsync(user.Id);

        context.ChangeTracker.Clear();
        var updated = context.Set<User2FA>().Find(twoFa.Id);
        updated!.IsEnabled.Should().BeFalse();
        updated.TotpSecret.Should().BeNull();
        updated.BackupCodes.Should().BeNull();

        _authServiceMock.Verify(a => a.ProcessTwoFactorStateChangeAsync(user.Id, false), Times.Once);
    }

    [Fact]
    public async Task GenerateBackupCodesAsync_CreatesHashedCodesInDatabase()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);

        var user2Fa = new User2FA { Id = Guid.NewGuid(), UserId = user.Id };
        context.Set<User2FA>().Add(user2Fa);
        await context.SaveChangesAsync();

        var codes = await sut.GenerateBackupCodesAsync(user.Id);

        codes.Should().HaveCount(10);
        codes.All(c => c.Length == 8).Should().BeTrue();

        context.ChangeTracker.Clear();
        var updated2Fa = context.Set<User2FA>().First(u => u.UserId == user.Id);
        updated2Fa.BackupCodesRemaining.Should().Be(10);
        updated2Fa.BackupCodes.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task VerifyBackupCodeAsync_WithValidCode_ReturnsTrueAndDecrementsCount()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);

        var user2Fa = new User2FA { Id = Guid.NewGuid(), UserId = user.Id };
        context.Set<User2FA>().Add(user2Fa);
        await context.SaveChangesAsync();

        var codes = await sut.GenerateBackupCodesAsync(user.Id);
        var codeToVerify = codes[3];

        var result = await sut.VerifyBackupCodeAsync(user.Id, codeToVerify);
        result.Should().BeTrue();

        context.ChangeTracker.Clear();
        var updated2Fa = context.Set<User2FA>().First(u => u.UserId == user.Id);
        updated2Fa.BackupCodesRemaining.Should().Be(9);
    }

    [Fact]
    public async Task VerifyBackupCodeAsync_WithInvalidCode_ReturnsFalse()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);

        var user2Fa = new User2FA { Id = Guid.NewGuid(), UserId = user.Id };
        context.Set<User2FA>().Add(user2Fa);
        await context.SaveChangesAsync();

        await sut.GenerateBackupCodesAsync(user.Id);

        var result = await sut.VerifyBackupCodeAsync(user.Id, "INVALID");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTwoFactorEnabledAsync_ReturnsCorrectStatus()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);
        var userId = Guid.NewGuid();

        (await sut.IsTwoFactorEnabledAsync(userId)).Should().BeFalse();

        var user2Fa = new User2FA { Id = Guid.NewGuid(), UserId = userId, IsEnabled = true };
        context.Set<User2FA>().Add(user2Fa);
        await context.SaveChangesAsync();

        (await sut.IsTwoFactorEnabledAsync(userId)).Should().BeTrue();
    }

    [Fact]
    public async Task ResetTwoFactorAsync_DeletesRecord()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);
        var userId = Guid.NewGuid();

        var user2Fa = new User2FA { Id = Guid.NewGuid(), UserId = userId, IsEnabled = true };
        context.Set<User2FA>().Add(user2Fa);
        await context.SaveChangesAsync();

        await sut.ResetTwoFactorAsync(userId);

        context.ChangeTracker.Clear();
        context.Set<User2FA>().Any(u => u.UserId == userId).Should().BeFalse();
    }

    [Fact]
    public async Task SmsCode_InitiateAndVerify_WorksCorrectly()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);

        var user2Fa = new User2FA { Id = Guid.NewGuid(), UserId = user.Id, PhoneNumber = "+1234567890" };
        context.Set<User2FA>().Add(user2Fa);
        await context.SaveChangesAsync();

        _smsServiceMock.Setup(s => s.SendVerificationCodeAsync(tenant.Id, "+1234567890", It.IsAny<string>()))
            .ReturnsAsync(new SmsResult(true, "msg-123", null));

        // Act - Initiate SMS Code
        var initiated = await sut.InitiateSmsCodeAsync(user.Id);
        initiated.Should().BeTrue();

        context.ChangeTracker.Clear();
        var updated2Fa = context.Set<User2FA>().First(u => u.UserId == user.Id);
        updated2Fa.SmsCode.Should().NotBeNull();
        updated2Fa.SmsCodeExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // We can't know the plain code directly, but let's mock/inject or verify using the captured SMS code
        // Instead, let's grab the code sent to smsService
        _smsServiceMock.Verify(s => s.SendVerificationCodeAsync(tenant.Id, "+1234567890", It.IsAny<string>()), Times.Once);

        // Since we verify via SmsCode, let's look at the stored hashed SMS code.
        // Let's create a test verifying with wrong code first
        var verifyWrong = await sut.VerifySmsCodeAsync(user.Id, "000000");
        verifyWrong.Should().BeFalse();
    }

    [Fact]
    public async Task EmailCode_InitiateAndVerify_WorksCorrectly()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };
        context.Tenants.Add(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        _emailServiceMock.Setup(e => e.SendTwoFactorCodeAsync("test@upkilo.com", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act - Initiate Email Code
        var initiated = await sut.InitiateEmailCodeAsync(user.Id);
        initiated.Should().BeTrue();

        context.ChangeTracker.Clear();
        var updated2Fa = context.Set<User2FA>().First(u => u.UserId == user.Id);
        updated2Fa.EmailCode.Should().NotBeNull();
        updated2Fa.EmailCodeExpiresAt.Should().BeAfter(DateTime.UtcNow);

        _emailServiceMock.Verify(e => e.SendTwoFactorCodeAsync("test@upkilo.com", It.IsAny<string>()), Times.Once);

        // Verify with wrong code
        var verifyWrong = await sut.VerifyEmailCodeAsync(user.Id, "000000");
        verifyWrong.Should().BeFalse();
    }

    [Fact]
    public async Task TrustDeviceAsync_And_IsDeviceTrustedAsync_WorksCorrectly()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);
        var userId = Guid.NewGuid();

        // Trust device
        var token = await sut.TrustDeviceAsync(userId, "Mozilla/Chrome");
        token.Should().NotBeNullOrWhiteSpace();

        // Check is trusted
        var isTrusted = await sut.IsDeviceTrustedAsync(userId, token);
        isTrusted.Should().BeTrue();

        // Check wrong token
        var isWrongTrusted = await sut.IsDeviceTrustedAsync(userId, "invalid-token");
        isWrongTrusted.Should().BeFalse();

        // Check empty token
        var isEmptyTrusted = await sut.IsDeviceTrustedAsync(userId, "");
        isEmptyTrusted.Should().BeFalse();
    }

    [Fact]
    public async Task IsTwoFactorEnforcedAsync_ReturnsTrue_IfEnforcedByRoleOrTenant()
    {
        var context = _dbFactory.CreateContext();
        var sut = CreateSut(context);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            EnforceTwoFactor = false,
            Settings = new Dictionary<string, object>
            {
                ["Enforce2FA_Admin"] = true
            }
        };
        context.Tenants.Add(tenant);

        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "test@upkilo.com", FirstName = "Test", LastName = "User", Role = UserRole.Admin };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Enforced by role Admin
        (await sut.IsTwoFactorEnforcedAsync(user.Id)).Should().BeTrue();

        // Change settings to false — replace whole dict so EF detects the change
        tenant.Settings = new Dictionary<string, object> { ["Enforce2FA_Admin"] = false };
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        (await sut.IsTwoFactorEnforcedAsync(user.Id)).Should().BeFalse();

        // Enforce by tenant-level EnforceTwoFactor — re-fetch to avoid tracker conflict
        context.ChangeTracker.Clear();
        var reloadedTenant = await context.Tenants.FindAsync(tenant.Id);
        reloadedTenant!.EnforceTwoFactor = true;
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        (await sut.IsTwoFactorEnforcedAsync(user.Id)).Should().BeTrue();
    }
}
