using System.Threading.Tasks;
using System;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
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

    public void Dispose() => _dbFactory.Dispose();
}
