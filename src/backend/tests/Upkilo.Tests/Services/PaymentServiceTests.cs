using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using MockFactory = Upkilo.Tests.Helpers.MockFactory;

namespace Upkilo.Tests.Services;

/// <summary>
/// Tests for PaymentService — Stripe integration with focus on the _isConfigured guard,
/// EnsureCustomerAsync DB interactions, and cross-tenant IDOR prevention.
/// Since PaymentService creates Stripe SDK clients internally, we test the guarded paths
/// that don't hit Stripe (unconfigured state) and DB-level logic.
/// </summary>
public class PaymentServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly PaymentService _sut;
    private readonly PaymentService _unconfiguredSut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PaymentServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        var context = _dbFactory.CreateContext();
        var logger = new Mock<ILogger<PaymentService>>();

        // Configured service (has Stripe key)
        var configuredSecretProvider = MockFactory.CreateSecretProvider();
        _sut = new PaymentService(
            MockFactory.CreateConfiguration(),
            logger.Object,
            context,
            configuredSecretProvider.Object);

        // Unconfigured service (no Stripe key)
        var unconfiguredSecretProvider = new Mock<ISecretProvider>();
        unconfiguredSecretProvider.Setup(s => s.GetSecret(It.Is<string>(k => k != "Stripe--SecretKey"))).Returns("test");
        unconfiguredSecretProvider.Setup(s => s.GetSecret("Stripe--SecretKey")).Returns((string?)null);
        _unconfiguredSut = new PaymentService(
            MockFactory.CreateConfiguration(),
            logger.Object,
            context,
            unconfiguredSecretProvider.Object);

        // Seed tenant
        var tenant = TestFixtures.CreateTenant(_tenantId);
        tenant.StripeCustomerId = "cus_existing_123";
        context.Tenants.Add(tenant);
        context.SaveChanges();
    }

    public void Dispose() => _dbFactory.Dispose();

    // ── _isConfigured Guard Tests ─────────────────────────────────────

    [Fact]
    public async Task CreateCheckoutSession_NotConfigured_ReturnsFailure()
    {
        var request = new CreateCheckoutRequest(Guid.NewGuid(), "price_123", "https://ok.com", "https://cancel.com");

        var result = await _unconfiguredSut.CreateCheckoutSessionAsync(request);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not configured");
    }

    [Fact]
    public async Task CreatePaymentIntent_NotConfigured_ReturnsFailure()
    {
        var request = new CreatePaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 50.00m, "usd", "Test");

        var result = await _unconfiguredSut.CreatePaymentIntentAsync(request);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not configured");
    }

    [Fact]
    public async Task CapturePayment_NotConfigured_ReturnsFalse()
    {
        var result = await _unconfiguredSut.CapturePaymentAsync("pi_test_123", _tenantId);

        result.Should().BeFalse();
    }



    [Fact]
    public async Task RefundPaymentWithTenant_NotConfigured_ReturnsFailure()
    {
        var request = new RefundRequest("pi_test_123");

        var result = await _unconfiguredSut.RefundPaymentAsync(request, _tenantId);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not configured");
    }

    [Fact]
    public async Task GetPaymentMethods_NotConfigured_ReturnsEmpty()
    {
        var result = await _unconfiguredSut.GetPaymentMethodsAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AttachPaymentMethod_NotConfigured_ReturnsFalse()
    {
        var result = await _unconfiguredSut.AttachPaymentMethodAsync(Guid.NewGuid(), "pm_test");

        result.Should().BeFalse();
    }

    // ── EnsureCustomerAsync Tests ─────────────────────────────────────

    [Fact]
    public async Task EnsureCustomer_ExistingCustomer_ReturnsExistingId()
    {
        var customerId = await _sut.EnsureCustomerAsync(_tenantId, "test@test.com", "Test");

        customerId.Should().Be("cus_existing_123");
    }

    [Fact]
    public async Task EnsureCustomer_TenantNotFound_ThrowsException()
    {
        var nonExistentTenantId = Guid.NewGuid();

        var act = async () => await _sut.EnsureCustomerAsync(nonExistentTenantId, "test@test.com", "Test");

        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public async Task EnsureCustomer_NoStripeCustomerAndNotConfigured_Throws()
    {
        // Create tenant without StripeCustomerId
        var context = _dbFactory.CreateContext();
        var newTenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant
        {
            Id = newTenantId,
            Name = "No Stripe Tenant",
            Slug = "no-stripe",
            Status = TenantStatus.Active,
            StripeCustomerId = null,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var act = async () => await _unconfiguredSut.EnsureCustomerAsync(newTenantId, "test@test.com", "Test");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not configured*");
    }

    // ── CreateConnectAccount Tests ────────────────────────────────────

    [Fact]
    public async Task CreateConnectAccount_TenantNotFound_Throws()
    {
        var act = async () => await _sut.CreateConnectAccountAsync(Guid.NewGuid(), "email@test.com");

        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateConnectOnboardingLink_NotConfigured_Throws()
    {
        var act = async () => await _unconfiguredSut.CreateConnectOnboardingLinkAsync("acct_test", "https://refresh", "https://return");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not configured*");
    }

    [Fact]
    public async Task CreateBillingPortal_NotConfigured_Throws()
    {
        var act = async () => await _unconfiguredSut.CreateBillingPortalSessionAsync(_tenantId, "https://return.com");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not configured*");
    }
}
