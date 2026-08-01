using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Covers deriving a tenant's currency from their connected Stripe account.
///
/// The bug these guard against: Connect accounts are created as Standard, so the tenant picks
/// their country during Stripe's hosted onboarding and the currency is unknown until it finishes.
/// Nothing read it back, so every tenant kept the "USD" entity default — a salon settling in
/// rupees advertised prices in dollars, and no error was ever raised.
/// </summary>
public class TenantCurrencySyncServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IPaymentService> _payments = new();
    private readonly Mock<ILogger<TenantCurrencySyncService>> _logger = new();

    public TenantCurrencySyncServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private TenantCurrencySyncService Sut(AppDbContext ctx) => new(ctx, _payments.Object, _logger.Object);

    private static Tenant NewTenant(string currency = "USD", string? connectId = "acct_test") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Salon",
        Slug = $"salon-{Guid.NewGuid():N}",
        Currency = currency,
        StripeConnectId = connectId
    };

    // ── The core fix ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_CompletedAccount_AdoptsAccountCurrency()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        // Stripe reports currency lowercase.
        var result = await Sut(ctx).ApplyAsync(tenant, "inr", detailsSubmitted: true);

        result.Changed.Should().BeTrue();
        result.Current.Should().Be("INR");
        tenant.Currency.Should().Be("INR");
    }

    [Fact]
    public async Task ApplyAsync_IncompleteOnboarding_LeavesCurrencyAlone()
    {
        // A half-onboarded account reports a placeholder currency. Writing it would swap one
        // wrong value for another.
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        var result = await Sut(ctx).ApplyAsync(tenant, "eur", detailsSubmitted: false);

        result.Changed.Should().BeFalse();
        result.Reason.Should().Be("onboarding_incomplete");
        tenant.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ApplyAsync_SameCurrency_IsNoOp()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        var result = await Sut(ctx).ApplyAsync(tenant, "USD", detailsSubmitted: true);

        result.Changed.Should().BeFalse();
        result.Reason.Should().Be("already_current");
    }

    [Fact]
    public async Task ApplyAsync_MissingAccountCurrency_LeavesCurrencyAlone()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        var result = await Sut(ctx).ApplyAsync(tenant, null, detailsSubmitted: true);

        result.Changed.Should().BeFalse();
        tenant.Currency.Should().Be("USD");
    }

    // ── Existing prices are reported, never rewritten ────────────────────

    [Fact]
    public async Task ApplyAsync_DoesNotConvertExistingServicePrices()
    {
        // Reinterpreting 500 from one currency to another silently changes every price on the
        // tenant's booking page. The amounts must stay untouched and be surfaced for review.
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        ctx.Services.Add(new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Haircut",
            Price = 30,
            Currency = "USD",
            DurationMinutes = 30
        });
        await ctx.SaveChangesAsync();

        var result = await Sut(ctx).ApplyAsync(tenant, "INR", detailsSubmitted: true);

        result.Changed.Should().BeTrue();
        result.StalePriceCount.Should().Be(1, "the tenant must be told which prices need revisiting");

        var service = ctx.Services.IgnoreQueryFilters().First(s => s.TenantId == tenant.Id);
        service.Price.Should().Be(30, "the amount must not be converted");
    }

    [Fact]
    public async Task ApplyAsync_FreeServices_AreNotCountedAsNeedingReview()
    {
        // A zero-priced service has no amount to reconsider; counting it would make the prompt
        // noisy enough to ignore.
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        ctx.Services.Add(new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Free consult",
            Price = 0,
            Currency = "USD",
            DurationMinutes = 15
        });
        await ctx.SaveChangesAsync();

        var result = await Sut(ctx).ApplyAsync(tenant, "INR", detailsSubmitted: true);

        result.StalePriceCount.Should().Be(0);
    }

    // ── Fetching from Stripe ─────────────────────────────────────────────

    [Fact]
    public async Task SyncFromStripeAsync_NoConnectedAccount_ReportsReason()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD", connectId: null);
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        var result = await Sut(ctx).SyncFromStripeAsync(tenant.Id);

        result.Changed.Should().BeFalse();
        result.Reason.Should().Be("no_connected_account");
    }

    [Fact]
    public async Task SyncFromStripeAsync_AccountUnavailable_DoesNotChangeCurrency()
    {
        // A tenant can revoke platform access, leaving an id that no longer resolves. That must
        // not blank or alter the currency they are already trading in.
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("INR");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        _payments.Setup(p => p.GetConnectAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ConnectAccountInfo?)null);

        var result = await Sut(ctx).SyncFromStripeAsync(tenant.Id);

        result.Changed.Should().BeFalse();
        result.Reason.Should().Be("account_unavailable");
        tenant.Currency.Should().Be("INR");
    }

    [Fact]
    public async Task SyncFromStripeAsync_CompletedAccount_AdoptsCurrency()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = NewTenant("USD");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        _payments.Setup(p => p.GetConnectAccountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ConnectAccountInfo("acct_test", "JP", "jpy", true, true));

        var result = await Sut(ctx).SyncFromStripeAsync(tenant.Id);

        result.Changed.Should().BeTrue();
        result.Current.Should().Be("JPY");
        tenant.Currency.Should().Be("JPY");
    }

    [Fact]
    public async Task SyncFromStripeAsync_UnknownTenant_DoesNotThrow()
    {
        var ctx = _dbFactory.CreateContext();

        var result = await Sut(ctx).SyncFromStripeAsync(Guid.NewGuid());

        result.Changed.Should().BeFalse();
        result.Reason.Should().Be("tenant_not_found");
    }
}
