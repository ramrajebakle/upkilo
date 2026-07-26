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
