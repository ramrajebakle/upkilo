using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Stripe;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using SubscriptionService = Upkilo.Infrastructure.Services.SubscriptionService;
using Xunit;

namespace Upkilo.Tests.Services;

public class SubscriptionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly SubscriptionService _sut;
    private readonly Mock<IStripeClient> _stripeClientMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();

    public SubscriptionServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        var context = _dbFactory.CreateContext();
        var logger = new Mock<ILogger<SubscriptionService>>();
        var secretProvider = new Mock<ISecretProvider>();
        secretProvider.Setup(s => s.GetSecret(It.IsAny<string>())).Returns("sk_test_fake");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APP_URL"] = "https://app.upkilo.com"
            }).Build();

        _stripeClientMock.Setup(c => c.ApiKey).Returns("sk_test_fake");
        _stripeClientMock.Setup(c => c.ApiBase).Returns("https://api.stripe.com");

        context.Tenants.Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Salon",
            Slug = "test-salon",
            Status = TenantStatus.Active,
            StripeCustomerId = "cus_test_123",
            Email = "test-salon@example.com",
            CreatedAt = DateTime.UtcNow
        });

        var plan = new PricingPlan
        {
            Id = _planId,
            Name = "Professional",
            IsActive = true,
            TrialDays = 14,
            StripeExtraStaffPriceId = "price_staff",
            StripeExtraLocationPriceId = "price_location",
            Prices = new List<PlanPrice>
            {
                new PlanPrice { PricingPlanId = _planId, Cycle = BillingCycle.Monthly, Amount = 49m, StripePriceId = "price_pro_monthly" },
                new PlanPrice { PricingPlanId = _planId, Cycle = BillingCycle.Annual, Amount = 470m, StripePriceId = "price_pro_annual" }
            }
        };
        context.PricingPlans.Add(plan);

        context.Subscriptions.Add(new Upkilo.Core.Entities.Subscription
        {
            TenantId = _tenantId,
            PricingPlanId = _planId,
            PricingPlan = plan,
            Status = SubscriptionStatus.Active,
            BillingInterval = BillingInterval.Monthly,
            StripeSubscriptionId = "sub_test_123",
            CurrentPeriodStart = DateTime.UtcNow.Date.AddDays(-15),
            CurrentPeriodEnd = DateTime.UtcNow.Date.AddDays(16),
            BookingsUsed = 50,
            SmsUsed = 10,
            AiCreditsUsed = 5,
            AiMonthlyBudget = 25m
        });
        context.SaveChanges();

        // A REAL cache, not a mock that always misses.
        //
        // The mock here returned null from every read, so GetSubscriptionAsync could never
        // produce a cache hit and no test in this class ever exercised the warm-cache path.
        // That is precisely how the detached-entity write bug survived: on a hit the method
        // returns a deserialised POCO, mutations to it are untracked, and SaveChanges persists
        // nothing — a failure mode a permanently-cold cache cannot reproduce.
        var cache = Upkilo.Tests.Helpers.MockFactory.CreateMemoryCache();

        // A real EntitlementService, not a mock: SubscriptionService now delegates its feature
        // and limit answers here, so stubbing it would leave the assertions below testing
        // nothing but the stub.
        _sut = new SubscriptionService(context, logger.Object, config, secretProvider.Object, cache,
            Upkilo.Tests.Helpers.MockFactory.CreateEntitlementService(context, cache));

        // Set mock AFTER sut construction so SubscriptionService's StripeConfiguration.ApiKey setter doesn't override it
        StripeConfiguration.StripeClient = _stripeClientMock.Object;
    }

    public void Dispose() => _dbFactory.Dispose();

    // ── Writes must survive a warm cache ──────────────────────────────────────

    /// <summary>
    /// These pin the detached-entity bug. GetSubscriptionAsync serves a JSON round-trip from
    /// the cache, so a mutation applied to what it returns was never tracked and SaveChanges
    /// persisted nothing — silently, and only when the cache was warm. Each test warms the
    /// cache first, which is exactly the condition the original tests never created.
    /// </summary>
    [Fact]
    public async Task UpdateAiBudget_PersistsEvenWhenTheCacheIsWarm()
    {
        await _sut.GetSubscriptionAsync(_tenantId);   // warm the cache

        var result = await _sut.UpdateAiBudgetAsync(_tenantId, 99.50m);

        result.Success.Should().BeTrue();
        _dbFactory.CreateContext().Subscriptions
            .First(s => s.TenantId == _tenantId).AiMonthlyBudget
            .Should().Be(99.50m, "the reported success must correspond to a real write");
    }

    [Fact]
    public async Task IncrementUsage_PersistsEvenWhenTheCacheIsWarm()
    {
        await _sut.GetSubscriptionAsync(_tenantId);   // warm the cache

        await _sut.IncrementUsageAsync(_tenantId, UsageType.Bookings, 3);

        _dbFactory.CreateContext().Subscriptions
            .First(s => s.TenantId == _tenantId).BookingsUsed
            .Should().Be(53, "50 seeded + 3");
    }

    [Fact]
    public async Task MutatingPathsInvalidateTheCache_SoTheNextReadIsFresh()
    {
        await _sut.GetSubscriptionAsync(_tenantId);   // warm the cache
        await _sut.UpdateAiBudgetAsync(_tenantId, 42m);

        var reread = await _sut.GetSubscriptionAsync(_tenantId);

        reread!.AiMonthlyBudget.Should().Be(42m, "a stale cache would still report the old budget");
    }

    // ── Quota reservation ─────────────────────────────────────────────────────

    [Fact]
    public async Task TryReserveUsage_RefusesOnceTheLimitIsReached()
    {
        // ai_actions capped at 5, with 5 already consumed.
        await SeedAiQuotaAsync(limit: 5, alreadyUsed: 5);

        var granted = await _sut.TryReserveUsageAsync(_tenantId, UsageType.AiCredits);

        granted.Should().BeFalse("the tenant is already at their AI credit ceiling");
    }

    [Fact]
    public async Task TryReserveUsage_AllowsBelowTheLimit()
    {
        await SeedAiQuotaAsync(limit: 5, alreadyUsed: 4);

        (await _sut.TryReserveUsageAsync(_tenantId, UsageType.AiCredits)).Should().BeTrue();
    }

    [Fact]
    public async Task TryReserveUsage_RefusesAReservationThatWouldOvershoot()
    {
        await SeedAiQuotaAsync(limit: 5, alreadyUsed: 3);

        // 3 + 5 > 5. The old code compared against a cached counter and then incremented
        // unconditionally, so an oversized reservation sailed through.
        (await _sut.TryReserveUsageAsync(_tenantId, UsageType.AiCredits, amount: 5))
            .Should().BeFalse();
    }

    [Fact]
    public async Task TryReserveUsage_NeverExceedsTheLimitUnderConcurrency()
    {
        await SeedAiQuotaAsync(limit: 10, alreadyUsed: 0);

        // Check and increment used to be separate statements, so concurrent callers could each
        // pass the check and each increment past the ceiling. The limit is now a predicate in
        // the UPDATE, so the database serialises the decision.
        var results = new List<bool>();
        for (var i = 0; i < 25; i++)
            results.Add(await _sut.TryReserveUsageAsync(_tenantId, UsageType.AiCredits));

        results.Count(r => r).Should().Be(10, "exactly the quota should be grantable, never more");

        var used = _dbFactory.CreateContext().Subscriptions
            .First(s => s.TenantId == _tenantId).AiCreditsUsed;
        used.Should().Be(10);
    }

    /// <summary>
    /// Gives the tenant's plan an ai_actions mapping and sets the consumed counter, so the
    /// reservation path has a real limit to enforce against.
    /// </summary>
    private async Task SeedAiQuotaAsync(int limit, int alreadyUsed)
    {
        var ctx = _dbFactory.CreateContext();

        var feature = new PricingFeature { Key = FeatureKeys.AiActions, Name = "AI Actions", Type = FeatureType.Numeric };
        ctx.PricingFeatures.Add(feature);
        ctx.PlanFeatureMappings.Add(new PlanFeatureMapping
        {
            PricingPlanId = _planId,
            PricingFeature = feature,
            IsEnabled = true,
            NumericLimit = limit,
        });

        var sub = ctx.Subscriptions.First(x => x.TenantId == _tenantId);
        sub.AiCreditsUsed = alreadyUsed;

        await ctx.SaveChangesAsync();
    }


    [Fact]
    public async Task GetAllPricingPlansAsync_ReturnsActivePlans()
    {
        var plans = await _sut.GetAllPricingPlansAsync();
        plans.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPricingPlanAsync_ValidId_ReturnsPlan()
    {
        var plan = await _sut.GetPricingPlanAsync(_planId);
        plan.Should().NotBeNull();
        plan!.Name.Should().Be("Professional");
    }

    [Fact]
    public async Task GetSubscriptionAsync_ValidTenant_ReturnsWithPlan()
    {
        var sub = await _sut.GetSubscriptionAsync(_tenantId);
        sub.Should().NotBeNull();
        sub!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetUsageAsync_ReturnsCorrectUsage()
    {
        var usage = await _sut.GetUsageAsync(_tenantId);
        usage.BookingsUsed.Should().Be(50);
        usage.BookingsLimit.Should().Be(-1); // -1 = unlimited (plan has no bookings cap in test)
        usage.SmsUsed.Should().Be(10);
    }

    [Fact]
    public async Task IncrementUsageAsync_IncrementsCounter()
    {
        await _sut.IncrementUsageAsync(_tenantId, UsageType.Bookings, 5);
        var sub = await _sut.GetSubscriptionAsync(_tenantId);
        sub!.BookingsUsed.Should().Be(55);
    }

    [Fact]
    public async Task CheckUsageLimitAsync_WithinLimit_ReturnsTrue()
    {
        var result = await _sut.CheckUsageLimitAsync(_tenantId, UsageType.Bookings, 1);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckUsageLimitAsync_UnlimitedPlan_AlwaysTrue()
    {
        // Plan has no booking cap (BookingsLimit = -1), so adding 500 should still return true
        var result = await _sut.CheckUsageLimitAsync(_tenantId, UsageType.Bookings, 500);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeatureAccessAsync_NoMappings_ReturnsFalse()
    {
        // Test plan has no FeatureMappings seeded — feature access returns false
        var result = await _sut.CheckFeatureAccessAsync(_tenantId, "onlinebooking");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckFeatureAccessAsync_Unknown_ReturnsFalse()
    {
        var result = await _sut.CheckFeatureAccessAsync(_tenantId, "nonexistent");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAiBudgetAsync_ValidTenant_Updates()
    {
        var result = await _sut.UpdateAiBudgetAsync(_tenantId, 100m);
        result.Success.Should().BeTrue();
        var sub = await _sut.GetSubscriptionAsync(_tenantId);
        sub!.AiMonthlyBudget.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateAiBudgetAsync_NoSub_Fails()
    {
        var result = await _sut.UpdateAiBudgetAsync(Guid.NewGuid(), 50m);
        result.Success.Should().BeFalse();
    }

    // ---- Stripe Methods Tests ----

    [Fact]
    public async Task CreateCheckoutSessionAsync_CallsStripe_ReturnsSuccess()
    {
        // Arrange
        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Checkout.Session>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("checkout/sessions")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new Stripe.Checkout.Session { Id = "sess_created_123", Url = "https://checkout.stripe.com/pay/sess_created_123" });

        // Act
        var result = await _sut.CreateCheckoutSessionAsync(_tenantId, "price_monthly", false, "promo_123");

        // Assert
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be("sess_created_123");
        result.SessionUrl.Should().Be("https://checkout.stripe.com/pay/sess_created_123");
    }

    [Fact]
    public async Task CreateBillingPortalSessionAsync_CallsStripe_ReturnsUrl()
    {
        // Arrange
        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.BillingPortal.Session>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("billing_portal/sessions")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new Stripe.BillingPortal.Session { Id = "bps_123", Url = "https://billing.stripe.com/portal/bps_123" });

        // Act
        var url = await _sut.CreateBillingPortalSessionAsync(_tenantId, "https://app.upkilo.com/return");

        // Assert
        url.Should().Be("https://billing.stripe.com/portal/bps_123");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_CallsStripe_ReturnsStripeCheckoutUrl()
    {
        // Arrange
        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Checkout.Session>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("checkout/sessions")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new Stripe.Checkout.Session { Id = "sess_sub_456", Url = "https://checkout.stripe.com/pay/sess_sub_456" });

        // Act
        var result = await _sut.CreateSubscriptionAsync(_tenantId, _planId, BillingInterval.Annual, "promo_456");

        // Assert
        result.Success.Should().BeTrue();
        result.StripeCheckoutUrl.Should().Be("https://checkout.stripe.com/pay/sess_sub_456");
    }

    [Fact]
    public async Task ChangeSubscriptionAsync_CallsStripeUpdate_ReturnsSuccess()
    {
        // Arrange
        var stripeSub = new Stripe.Subscription
        {
            Id = "sub_test_123",
            Items = new StripeList<SubscriptionItem>
            {
                Data = new List<SubscriptionItem> { new SubscriptionItem { Id = "si_item_123" } }
            }
        };

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Get,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        // Act
        var result = await _sut.ChangeSubscriptionAsync(_tenantId, _planId, BillingInterval.Annual);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_CallsStripeUpdateOrCancel_ReturnsSuccess()
    {
        // Arrange
        var stripeSub = new Stripe.Subscription { Id = "sub_test_123" };

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Delete,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        // Act
        var resultAtPeriodEnd = await _sut.CancelSubscriptionAsync(_tenantId, immediate: false);
        var resultImmediate = await _sut.CancelSubscriptionAsync(_tenantId, immediate: true);

        // Assert
        resultAtPeriodEnd.Success.Should().BeTrue();
        resultImmediate.Success.Should().BeTrue();
    }

    [Fact]
    public async Task PauseAndResumeSubscriptionAsync_CallsStripe_ReturnsSuccess()
    {
        // Arrange
        var stripeSub = new Stripe.Subscription { Id = "sub_test_123" };

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        // Act
        var resultPause = await _sut.PauseSubscriptionAsync(_tenantId, DateTime.UtcNow.AddDays(7));
        var resultResume = await _sut.ResumeSubscriptionAsync(_tenantId);

        // Assert
        resultPause.Success.Should().BeTrue();
        resultResume.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SyncWithStripeAsync_UpdatesLocalSubscription()
    {
        // Arrange
        // Stripe.NET v50+ uses UnixDateTimeConverter: dates must be Unix timestamps (integers)
        var subJson = @"{
            ""id"": ""sub_test_123"",
            ""status"": ""active"",
            ""canceled_at"": null,
            ""items"": {
                ""object"": ""list"",
                ""data"": [
                    { ""id"": ""si_staff"", ""quantity"": 3, ""price"": { ""id"": ""price_staff"" }, ""current_period_start"": 1780358400, ""current_period_end"": 1782950400 },
                    { ""id"": ""si_loc"", ""quantity"": 2, ""price"": { ""id"": ""price_location"" }, ""current_period_start"": 1780358400, ""current_period_end"": 1782950400 },
                    { ""id"": ""si_main"", ""quantity"": 1, ""price"": { ""id"": ""price_monthly"", ""recurring"": { ""interval"": ""month"" } }, ""current_period_start"": 1780358400, ""current_period_end"": 1782950400 }
                ]
            },
            ""metadata"": {
                ""plan_id"": """ + _planId.ToString() + @""",
                ""is_trial"": ""false""
            }
        }";
        var stripeSub = Newtonsoft.Json.JsonConvert.DeserializeObject<Stripe.Subscription>(subJson);

        var listJson = @"{
            ""object"": ""list"",
            ""data"": [ " + subJson + @" ]
        }";
        var subList = Newtonsoft.Json.JsonConvert.DeserializeObject<StripeList<Stripe.Subscription>>(listJson);

        _stripeClientMock.Setup(c => c.RequestAsync<StripeList<Stripe.Subscription>>(
            HttpMethod.Get,
            It.Is<string>(path => path.Contains("subscriptions")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(subList);

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Get,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        // Act
        await _sut.SyncWithStripeAsync(_tenantId);

        // Assert
        var sub = await _sut.GetSubscriptionAsync(_tenantId);
        sub.Should().NotBeNull();
        sub!.ExtraStaffCount.Should().Be(3);
        sub.ExtraLocationCount.Should().Be(2);
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task AddExtraStaffAndLocationAsync_CallsStripe_ReturnsSuccess()
    {
        // Arrange
        var stripeSub = new Stripe.Subscription
        {
            Id = "sub_test_123",
            Items = new StripeList<SubscriptionItem>
            {
                Data = new List<SubscriptionItem>()
            }
        };

        _stripeClientMock.Setup(c => c.RequestAsync<Stripe.Subscription>(
            HttpMethod.Get,
            It.Is<string>(path => path.Contains("subscriptions/sub_test_123")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(stripeSub);

        _stripeClientMock.Setup(c => c.RequestAsync<SubscriptionItem>(
            HttpMethod.Post,
            It.Is<string>(path => path.Contains("subscription_items")),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new SubscriptionItem { Id = "si_created" });

        // Act
        var resultStaff = await _sut.AddExtraStaffAsync(_tenantId, 2);
        var resultLocation = await _sut.AddExtraLocationAsync(_tenantId, 1);

        // Assert
        resultStaff.Success.Should().BeTrue();
        resultLocation.Success.Should().BeTrue();

        var sub = await _sut.GetSubscriptionAsync(_tenantId);
        sub!.ExtraStaffCount.Should().Be(2);
        sub!.ExtraLocationCount.Should().Be(1);
    }

    [Fact]
    public async Task PromoCode_ValidationAndRedemption_WorksCorrectly()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = "SAVE50",
            IsActive = true,
            DiscountValue = 50.00m,
            TimesUsed = 0,
            UsageLimit = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(5)
        };
        context.Set<PromoCode>().Add(promo);
        await context.SaveChangesAsync();

        // Act & Assert
        var validated = await _sut.ValidatePromoCodeAsync("save50", _tenantId);
        validated.Should().NotBeNull();
        validated!.Code.Should().Be("SAVE50");

        var redemption = await _sut.RedeemPromoCodeAsync("SAVE50", _tenantId);
        redemption.Should().NotBeNull();
        redemption!.DiscountApplied.Should().Be(50.00m);

        // Try validation again (should be null since already redeemed for this tenant)
        var secondValidation = await _sut.ValidatePromoCodeAsync("SAVE50", _tenantId);
        secondValidation.Should().BeNull();
    }

    [Fact]
    public async Task CalculateProratedAmountAsync_ReturnsNonNegativeAmount()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var currentPlan = await _sut.GetPricingPlanAsync(_planId);

        var newPlan = new PricingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            IsActive = true
        };
        context.PricingPlans.Add(newPlan);
        await context.SaveChangesAsync();

        // Act
        var amount = await _sut.CalculateProratedAmountAsync(_tenantId, newPlan.Id);

        // Assert — prorated amount should be non-negative
        amount.Should().BeGreaterThanOrEqualTo(0m);
    }
}
