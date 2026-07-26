using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class MarketingAutomationServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<MarketingAutomationService>> _loggerMock = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<IMarketingIntegrationService> _integrationMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    public MarketingAutomationServiceTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Generated content." });

        _loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    private MarketingAutomationService CreateSut()
    {
        return new MarketingAutomationService(
            _dbFactory.CreateContext(),
            _loggerMock.Object,
            _aiServiceMock.Object,
            _integrationMock.Object,
            _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_NoData_ReturnsDto()
    {
        var sut = CreateSut();

        var result = await sut.GetDashboardAsync(Guid.NewGuid());

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecentActionsAsync_NoActions_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.GetRecentActionsAsync(Guid.NewGuid(), 10);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
