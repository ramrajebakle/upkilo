using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class AiServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ISubscriptionService> _subscriptionMock = new();
    private readonly Mock<IContentModerationService> _contentModerationMock = new();
    private readonly Mock<ILogger<AiService>> _loggerMock = new();

    public AiServiceTests()
    {
        _contentModerationMock
            .Setup(m => m.ModerateTextAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModerationResult.Allowed());
    }

    private AiService CreateSut(IConfiguration? config = null)
    {
        config ??= new ConfigurationBuilder().Build();
        var modelResolver = new Mock<IAiModelResolver>();
        modelResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>()))
            .ReturnsAsync("claude-haiku-4-5-20251001");
        modelResolver.Setup(r => r.ResolveForTier(It.IsAny<string>()))
            .Returns("claude-haiku-4-5-20251001");
        return new AiService(
            _dbFactory.CreateContext(),
            config,
            _loggerMock.Object,
            _subscriptionMock.Object,
            _contentModerationMock.Object,
            modelResolver.Object);
    }

    [Fact]
    public async Task GenerateTextAsync_NoAzureConfig_ReturnsFailureResult()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        var result = await sut.GenerateTextAsync(tenantId, null, "Hello");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CheckSafetyAsync_ContentBlocked_ReturnsFalse()
    {
        _contentModerationMock
            .Setup(m => m.ModerateTextAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsAllowed = false });

        var sut = CreateSut();

        var isSafe = await sut.CheckSafetyAsync("malicious content here");

        isSafe.Should().BeFalse();
    }

    public void Dispose() => _dbFactory.Dispose();
}
