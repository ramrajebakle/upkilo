using System.Threading.Tasks;
using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class AzureOpenAIServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<AzureOpenAIService>> _loggerMock = new();
    private readonly HttpClient _httpClientMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<IContentModerationService> _contentModerationMock = new();
    private readonly Mock<ISecretProvider> _secretProviderMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<StackExchange.Redis.IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IPiiScrubberService> _piiScrubberMock = new();

    public AzureOpenAIServiceTests()
    {
        var dbContext = _dbFactory.CreateContext();
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(AppDbContext)))
            .Returns(dbContext);
        _scopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);
        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(_scopeMock.Object);
    }

    private AzureOpenAIService CreateSut(IConfiguration? config = null)
    {
        config ??= new ConfigurationBuilder().Build();
        var modelResolver = new Mock<IAiModelResolver>();
        modelResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>()))
            .ReturnsAsync("claude-haiku-4-5-20251001");
        modelResolver.Setup(r => r.ResolveForTier(It.IsAny<string>()))
            .Returns("claude-haiku-4-5-20251001");
        return new AzureOpenAIService(
            _dbFactory.CreateContext(),
            config,
            _secretProviderMock.Object,
            _loggerMock.Object,
            _httpClientMock,
            _cacheMock.Object,
            _notificationMock.Object,
            _contentModerationMock.Object,
            _scopeFactoryMock.Object,
            _redisMock.Object,
            _piiScrubberMock.Object,
            modelResolver.Object);
    }

    /// <summary>
    /// A tenant with no Subscription row must still be able to use a default model.
    ///
    /// Whether a tenant may use AI is decided by the entitlement engine and the
    /// [RequiresFeature]/[FeatureGuard] gate on the controller. IsModelAllowedAsync only
    /// chooses WHICH MODEL. It used to return false for a null subscription, which made it a
    /// second gate answering a question that was not its own — and answering it differently.
    ///
    /// That contradiction is reachable: a TenantFeatureOverride deliberately outranks the
    /// subscription lifecycle, so an admin can grant ai_copilot to a tenant with no
    /// subscription. The request passed the controller gate and was then refused here as
    /// "model not allowed", naming the wrong cause.
    /// </summary>
    [Fact]
    public async Task IsModelAllowedAsync_WithNoSubscription_AllowsADefaultModel()
    {
        var sut = CreateSut();

        var allowed = await sut.IsModelAllowedAsync(Guid.NewGuid(), "gpt-5-mini");

        allowed.Should().BeTrue(
            "entitlement decides IF the tenant gets AI; this method only decides WHICH model");
    }

    /// <summary>
    /// The loosening above must not become "any model is allowed". An unknown or more
    /// expensive model is still refused, so the allowlist keeps its purpose.
    /// </summary>
    [Fact]
    public async Task IsModelAllowedAsync_WithNoSubscription_StillRefusesAnUnlistedModel()
    {
        var sut = CreateSut();

        var allowed = await sut.IsModelAllowedAsync(Guid.NewGuid(), "gpt-5-turbo-unlisted");

        allowed.Should().BeFalse("the default list is an allowlist, not an open door");
    }

    /// <summary>
    /// The models AiModelResolver hands out must all survive this allowlist. When the two
    /// drifted apart, the resolver returned a model that was then rejected before dispatch and
    /// AI failed for the tiers using it, with nothing pointing at the mismatch.
    /// </summary>
    [Theory]
    [InlineData("gpt-5-mini")]    // EconomyModel  - Free / Starter
    [InlineData("gpt-5.4-mini")]  // StandardModel - Growth / Enterprise
    public async Task IsModelAllowedAsync_AcceptsEveryModelTheResolverCanReturn(string model)
    {
        var sut = CreateSut();

        (await sut.IsModelAllowedAsync(Guid.NewGuid(), model)).Should().BeTrue(
            $"AiModelResolver can return '{model}', so dispatch must not reject it");
    }

    [Fact]
    public void Instantiation_WithEmptyConfig_DoesNotThrow()
    {
        var act = () => CreateSut();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GenerateTextAsync_NoEndpoint_ReturnsFailureResult()
    {
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        var result = await sut.GenerateTextAsync(tenantId, null, "test prompt");

        // Without a subscription quota seeded, CheckQuotaAsync returns false
        result.Success.Should().BeFalse();
    }

    // ── Platform assistant quota ─────────────────────────────────────────────────────────────
    //
    // Upkilo's own marketing-site assistant (PublicSupportController) runs under a well-known id
    // that deliberately has NO Tenants row and no Subscription. The generic quota path rejects a
    // missing subscription outright, so without an explicit branch the public support bot failed
    // every request with "AI quota exceeded" and only ever emitted its fallback message.
    //
    // These tests are on AzureOpenAIService specifically because that is the class registered as
    // IAIService. The near-identical AiService is not registered anywhere, so a fix applied there
    // has no runtime effect — which is exactly how this defect survived the first pass.

    /// <summary>Points the Redis mock at a fixed current spend for the platform counter.</summary>
    private void SeedPlatformSpend(decimal spend)
    {
        var db = new Mock<StackExchange.Redis.IDatabase>();
        db.Setup(d => d.StringGetAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync((StackExchange.Redis.RedisValue)spend.ToString(
                System.Globalization.CultureInfo.InvariantCulture));

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(db.Object);
    }

    private static IConfiguration BudgetConfig(string budget) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:PlatformMonthlyBudget"] = budget,
            })
            .Build();

    [Fact]
    public async Task CheckQuotaAsync_PlatformAssistantUnderBudget_IsAllowedWithoutASubscription()
    {
        SeedPlatformSpend(1.00m);

        var allowed = await CreateSut(BudgetConfig("25.00")).CheckQuotaAsync(UpkiloPlatform.TenantId);

        allowed.Should().BeTrue(
            "the visitor using the public support bot is not a customer, so there is no "
            + "subscription to check — rejecting here silences the bot entirely");
    }

    [Fact]
    public async Task CheckQuotaAsync_PlatformAssistantOverBudget_IsRefused()
    {
        SeedPlatformSpend(30.00m);

        var allowed = await CreateSut(BudgetConfig("25.00")).CheckQuotaAsync(UpkiloPlatform.TenantId);

        allowed.Should().BeFalse(
            "the budget cap is the abuse control on an endpoint anyone on the internet can reach");
    }

    [Fact]
    public async Task CheckQuotaAsync_PlatformBudgetOfZero_TurnsTheSupportBotOff()
    {
        SeedPlatformSpend(0m);

        var allowed = await CreateSut(BudgetConfig("0")).CheckQuotaAsync(UpkiloPlatform.TenantId);

        allowed.Should().BeFalse();
    }

    /// <summary>
    /// A Redis counter that actually holds state, starting ABSENT — as it would after an
    /// eviction, a flush, or a restart without persistence. Stateful rather than a bare stub so
    /// the test can assert on the resulting BEHAVIOUR (was the call refused?) instead of on which
    /// StringSetAsync overload happened to be chosen.
    /// </summary>
    private Mock<StackExchange.Redis.IDatabase> StatefulMissingCounter()
    {
        var db = new Mock<StackExchange.Redis.IDatabase>();
        double value = 0;
        bool exists = false;

        db.Setup(d => d.KeyExistsAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(() => exists);

        // StringSetAsync has two overloads (with and without keepTtl) and the compiler's choice is
        // not obvious from the call site. Stubbing only one silently no-ops the seed and the test
        // passes for the wrong reason, so both are wired to the same state.
        db.Setup(d => d.StringSetAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(),
                It.IsAny<StackExchange.Redis.RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<StackExchange.Redis.When>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync((StackExchange.Redis.RedisKey _, StackExchange.Redis.RedisValue v,
                           TimeSpan? _, StackExchange.Redis.When _, StackExchange.Redis.CommandFlags _) =>
            {
                value = (double)v; exists = true; return true;
            });

        db.Setup(d => d.StringSetAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(),
                It.IsAny<StackExchange.Redis.RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<StackExchange.Redis.When>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync((StackExchange.Redis.RedisKey _, StackExchange.Redis.RedisValue v,
                           TimeSpan? _, bool _, StackExchange.Redis.When _,
                           StackExchange.Redis.CommandFlags _) =>
            {
                value = (double)v; exists = true; return true;
            });

        db.Setup(d => d.StringIncrementAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<double>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync((StackExchange.Redis.RedisKey _, double delta,
                           StackExchange.Redis.CommandFlags _) =>
            {
                value += delta; exists = true; return value;
            });

        db.Setup(d => d.KeyExpireAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<TimeSpan?>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(true);

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        return db;
    }

    /// <summary>Builds the service against a live-ish HTTP layer and the given config.</summary>
    private AzureOpenAIService CreateConfiguredSut(string budget)
    {
        _piiScrubberMock.Setup(p => p.Scrub(It.IsAny<string>())).Returns<string>(s => s);
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://unit-test.openai.azure.com",
                ["AzureOpenAI:ApiKey"] = "unit-test-key",
                ["Ai:PlatformMonthlyBudget"] = budget,
            })
            .Build();

        var modelResolver = new Mock<IAiModelResolver>();
        modelResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("gpt-5-mini");

        return new AzureOpenAIService(
            _dbFactory.CreateContext(), config, _secretProviderMock.Object, _loggerMock.Object,
            new HttpClient(new CapturingHandler()), _cacheMock.Object, _notificationMock.Object,
            _contentModerationMock.Object, _scopeFactoryMock.Object, _redisMock.Object,
            _piiScrubberMock.Object, modelResolver.Object);
    }

    private async Task SeedPlatformUsageLogAsync(decimal cost)
    {
        var db = _dbFactory.CreateContext();
        db.Set<AIUsageLog>().Add(new AIUsageLog
        {
            Id = Guid.NewGuid(),
            TenantId = UpkiloPlatform.TenantId,
            Model = "gpt-5-mini",
            Feature = UpkiloPlatform.UsageFeature,
            Cost = cost,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        _serviceProviderMock.Setup(x => x.GetService(typeof(AppDbContext))).Returns(db);
    }

    [Fact]
    public async Task PlatformQuota_WhenRedisCounterIsLost_IsRebuiltFromTheDurableUsageLog()
    {
        // THE regression this guards. The cap lived only in a Redis key created without a TTL,
        // under a maxmemory-policy of allkeys-lru. Losing that key - eviction, flush, or a restart
        // without persistence - reset the month's spend to zero and handed an anonymous,
        // internet-reachable endpoint another full budget.
        //
        // $30 has already been spent this month and Redis has lost the counter. The service must
        // read that total from AIUsageLogs (the durable record) and write it back, so the cap
        // resumes from $30 rather than from $0. Asserting on the value written is the right level:
        // Redis's own arithmetic after that point belongs to Redis, not to this test.
        await SeedPlatformUsageLogAsync(30.00m);
        var redis = StatefulMissingCounter();

        await CreateConfiguredSut("25.00")
            .GenerateTextAsync(UpkiloPlatform.TenantId, null, "hello", "gpt-5-mini");

        var seeded = redis.Invocations
            .Where(i => i.Method.Name == nameof(StackExchange.Redis.IDatabase.StringSetAsync))
            .Select(i => (double)(StackExchange.Redis.RedisValue)i.Arguments[1]!)
            .ToList();

        seeded.Should().ContainSingle().Which.Should().Be(30.0d,
            "a lost counter must be rebuilt from AIUsageLogs, not restarted at zero");
    }

    [Fact]
    public async Task PlatformQuota_WhenRedisCounterIsPresent_DoesNotRequeryTheUsageLog()
    {
        // The reseed must be the exception, not a per-call database read on the hot path.
        var db = new Mock<StackExchange.Redis.IDatabase>();
        db.Setup(d => d.KeyExistsAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(true);
        db.Setup(d => d.StringIncrementAsync(
                It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<double>(),
                It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(1.0d);
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        await SeedPlatformUsageLogAsync(30.00m);

        await CreateConfiguredSut("25.00")
            .GenerateTextAsync(UpkiloPlatform.TenantId, null, "hello", "gpt-5-mini");

        db.Invocations.Count(i => i.Method.Name == nameof(StackExchange.Redis.IDatabase.StringSetAsync))
            .Should().Be(0, "the counter was present, so there was nothing to rebuild");
    }

    [Fact]
    public async Task CheckQuotaAsync_OrdinaryTenantWithNoSubscription_IsStillRefused()
    {
        // The platform branch must not have widened the rule for everyone else.
        var allowed = await CreateSut(BudgetConfig("25.00")).CheckQuotaAsync(Guid.NewGuid());

        allowed.Should().BeFalse();
    }

    // ── Metered AI billing ───────────────────────────────────────────────────────────────────
    //
    // Reporting AI overage to Stripe only ever existed in AiService, which DI never constructed,
    // so PricingPlan.StripeAiUsagePriceId was admin-configurable and read by nothing. It is now
    // on the live path but gated, because enabling it invoices real customers.

    [Fact]
    public void ReportAiUsage_IsOffUnlessExplicitlyEnabled()
    {
        // The default must be "do not bill". A missing key reading as true would start charging
        // every tenant whose plan has an AI usage price the moment this shipped.
        var config = new ConfigurationBuilder().Build();

        config.GetValue<bool>("Billing:ReportAiUsage").Should().BeFalse();
    }

    [Fact]
    public void ReportAiUsage_ReadsTheFlagWhenSet()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Billing:ReportAiUsage"] = "true",
            })
            .Build();

        config.GetValue<bool>("Billing:ReportAiUsage").Should().BeTrue();
    }

    [Fact]
    public async Task GenerateTextAsync_WithBillingEnabled_StillDoesNotBillAFailedCall()
    {
        // No endpoint configured, so generation fails. A failed turn must never be invoiced —
        // the guard is `success && cost > 0`, not the flag alone.
        var subscriptions = new Mock<ISubscriptionService>();
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ISubscriptionService)))
            .Returns(subscriptions.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Billing:ReportAiUsage"] = "true",
            })
            .Build();

        await CreateSut(config).GenerateTextAsync(Guid.NewGuid(), null, "prompt");

        subscriptions.Verify(
            s => s.ReportUsageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>()),
            Times.Never);
    }

    // ── Model payload compatibility ──────────────────────────────────────────────────────────
    //
    // These assert the JSON actually put on the wire, because that is where the failure was.
    // Both request builders hardcoded temperature = 0.7, and the gpt-5 family rejects ANY
    // explicit temperature:
    //   "Unsupported value: 'temperature' does not support 0.7 with this model.
    //    Only the default (1) value is supported."   (HTTP 400)
    // AiModelResolver returns nothing but gpt-5 models, so every text generation in the product
    // failed before reaching the model. No mocked IAIService test could catch it — only the real
    // endpoint rejects the payload — so these tests inspect the serialised request instead.

    /// <summary>Captures the outgoing request body and returns a canned completion.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"choices":[{"message":{"content":"ok"}}],
                     "usage":{"prompt_tokens":10,"completion_tokens":5}}
                    """,
                    System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    /// <summary>Configured so the call actually reaches the HTTP layer, on the platform identity
    /// (which my quota branch admits without a Subscription row).</summary>
    private AzureOpenAIService CreateSutWithHandler(CapturingHandler handler)
    {
        SeedPlatformSpend(0m);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://unit-test.openai.azure.com",
                ["AzureOpenAI:ApiKey"] = "unit-test-key",
                ["Ai:PlatformMonthlyBudget"] = "25.00",
            })
            .Build();

        var modelResolver = new Mock<IAiModelResolver>();
        modelResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("gpt-5-mini");
        modelResolver.Setup(r => r.ResolveForTier(It.IsAny<string>())).Returns("gpt-5-mini");

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        return new AzureOpenAIService(
            _dbFactory.CreateContext(), config, _secretProviderMock.Object, _loggerMock.Object,
            new HttpClient(handler), _cacheMock.Object, _notificationMock.Object,
            _contentModerationMock.Object, _scopeFactoryMock.Object, _redisMock.Object,
            _piiScrubberMock.Object, modelResolver.Object);
    }

    [Fact]
    public async Task GenerateTextAsync_Gpt5Model_OmitsTemperatureFromTheRequest()
    {
        _piiScrubberMock.Setup(p => p.Scrub(It.IsAny<string>())).Returns<string>(s => s);
        var handler = new CapturingHandler();

        await CreateSutWithHandler(handler)
            .GenerateTextAsync(UpkiloPlatform.TenantId, null, "hello", "gpt-5-mini");

        handler.Body.Should().NotBeNull("the call must reach the HTTP layer for this to mean anything");
        handler.Body!.Should().NotContain("temperature",
            "the gpt-5 family rejects any explicit temperature with HTTP 400");
    }

    [Fact]
    public async Task GenerateTextAsync_Gpt5Model_SendsMaxCompletionTokensNotMaxTokens()
    {
        _piiScrubberMock.Setup(p => p.Scrub(It.IsAny<string>())).Returns<string>(s => s);
        var handler = new CapturingHandler();

        await CreateSutWithHandler(handler)
            .GenerateTextAsync(UpkiloPlatform.TenantId, null, "hello", "gpt-5-mini");

        handler.Body!.Should().Contain("max_completion_tokens");
        // "max_tokens" is a substring of "max_completion_tokens", so match the JSON key exactly.
        handler.Body.Should().NotContain("\"max_tokens\"");
    }

    [Fact]
    public async Task GenerateTextAsync_LegacyModel_StillSendsTemperature()
    {
        // The predicate exists rather than deleting temperature outright: gpt-4 and gpt-3.5-turbo
        // remain in IsModelAllowedAsync's default list and do honour it.
        _piiScrubberMock.Setup(p => p.Scrub(It.IsAny<string>())).Returns<string>(s => s);
        var handler = new CapturingHandler();

        await CreateSutWithHandler(handler)
            .GenerateTextAsync(UpkiloPlatform.TenantId, null, "hello", "gpt-4");

        handler.Body!.Should().Contain("temperature");
    }

    public void Dispose() => _dbFactory.Dispose();
}
