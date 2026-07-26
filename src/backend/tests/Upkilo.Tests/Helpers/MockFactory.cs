using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Tests.Helpers;

/// <summary>
/// Centralised mock factory — eliminates repetitive mock setup across test classes.
/// Every mock returns safe defaults (no nulls, no throws) unless overridden.
/// </summary>
public static class MockFactory
{
    // ── Configuration ─────────────────────────────────────────────────

    public static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "ThisIsAVeryLongTestSecretKeyThatIsAtLeast32CharactersLong!",
            ["Jwt:Issuer"] = "Upkilo.Tests",
            ["Jwt:Audience"] = "Upkilo.Tests",
            ["Jwt:ExpiryMinutes"] = "60",
            ["APP_URL"] = "https://app.upkilo.com",
            ["Stripe:WebhookSecret"] = "whsec_test_secret",
        };

        if (overrides != null)
        {
            foreach (var kv in overrides) defaults[kv.Key] = kv.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .Build();
    }

    // ── Secret Provider ───────────────────────────────────────────────

    public static Mock<ISecretProvider> CreateSecretProvider()
    {
        var mock = new Mock<ISecretProvider>();
        mock.Setup(s => s.GetSecret(It.IsAny<string>())).Returns("test-secret-value");
        mock.Setup(s => s.GetSecret("Jwt:Secret")).Returns("ThisIsAVeryLongTestSecretKeyThatIsAtLeast32CharactersLong!");
        mock.Setup(s => s.GetSecret("Stripe--SecretKey")).Returns("sk_test_fake");
        mock.Setup(s => s.GetSecretAsync(It.IsAny<string>())).ReturnsAsync("test-secret-value");
        mock.Setup(s => s.GetSecretAsync("Stripe:WebhookSecret")).ReturnsAsync("whsec_test_secret");
        return mock;
    }

    // ── Email Service ─────────────────────────────────────────────────

    public static Mock<IEmailService> CreateEmailService()
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(e => e.SendBookingConfirmationAsync(It.IsAny<BookingEmailData>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendBookingReminderAsync(It.IsAny<BookingEmailData>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendBookingCancellationAsync(It.IsAny<BookingEmailData>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendTwoFactorCodeAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mock.Setup(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        return mock;
    }

    // ── Scheduling Service ────────────────────────────────────────────

    public static Mock<ISchedulingService> CreateSchedulingService(bool slotsAvailable = true, bool concurrencyOk = true)
    {
        var mock = new Mock<ISchedulingService>();
        mock.Setup(s => s.CheckConcurrencyLimitAsync(It.IsAny<Guid>())).ReturnsAsync(concurrencyOk);
        mock.Setup(s => s.IsSlotAvailableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(slotsAvailable);
        mock.Setup(s => s.UpdateAvailabilityCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>()))
            .Returns(Task.CompletedTask);
        mock.Setup(s => s.InvalidateStaffCacheAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly?>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    // ── Event Service ─────────────────────────────────────────────────

    public static Mock<IEventService> CreateEventService()
    {
        var mock = new Mock<IEventService>();
        mock.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        return mock;
    }

    // ── Two-Factor Service ────────────────────────────────────────────

    public static Mock<ITwoFactorService> CreateTwoFactorService()
    {
        var mock = new Mock<ITwoFactorService>();
        return mock;
    }

    // ── Subscription Service ──────────────────────────────────────────

    public static Mock<ISubscriptionService> CreateSubscriptionService()
    {
        var mock = new Mock<ISubscriptionService>();
        mock.Setup(s => s.CheckFeatureAccessAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(true);
        mock.Setup(s => s.CheckUsageLimitAsync(It.IsAny<Guid>(), It.IsAny<UsageType>(), It.IsAny<int>())).ReturnsAsync(true);
        mock.Setup(s => s.IncrementUsageAsync(It.IsAny<Guid>(), It.IsAny<UsageType>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        return mock;
    }

    // ── Tenant Provider ───────────────────────────────────────────────

    public static Mock<ITenantProvider> CreateTenantProvider(Guid tenantId)
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(t => t.GetTenantId()).Returns(tenantId);
        return mock;
    }

    // ── Distributed Cache ─────────────────────────────────────────────

    public static Mock<IDistributedCache> CreateDistributedCache()
    {
        var mock = new Mock<IDistributedCache>();
        return mock;
    }

    // ── Business Metrics ──────────────────────────────────────────────

    public static Mock<IBusinessMetrics> CreateBusinessMetrics()
    {
        return new Mock<IBusinessMetrics>();
    }

    // ── HTTP Context Accessor ─────────────────────────────────────────

    public static Mock<IHttpContextAccessor> CreateHttpContextAccessor(Guid? tenantId = null, Guid? userId = null)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        if (tenantId.HasValue)
            context.Items["TenantId"] = tenantId.Value.ToString();
        if (userId.HasValue)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString())
            };
            context.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(claims, "test"));
        }
        mock.Setup(a => a.HttpContext).Returns(context);
        return mock;
    }

    // ── SMS Service ───────────────────────────────────────────────────

    public static Mock<ISmsService> CreateSmsService()
    {
        var mock = new Mock<ISmsService>();
        return mock;
    }

    // ── Payment Service ───────────────────────────────────────────────

    public static Mock<IPaymentService> CreatePaymentService()
    {
        var mock = new Mock<IPaymentService>();
        return mock;
    }

    // ── Db Connection Selector ────────────────────────────────────────

    public static Mock<IDbConnectionSelector> CreateDbConnectionSelector()
    {
        return new Mock<IDbConnectionSelector>();
    }

    // ── Generic Logger ────────────────────────────────────────────────

    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}
