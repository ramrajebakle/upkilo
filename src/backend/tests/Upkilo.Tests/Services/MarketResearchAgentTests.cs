using FluentAssertions;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class MarketResearchAgentTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<IAIDashboardService> _dashboardMock = new();

    public MarketResearchAgentTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Market analysis: strong competition in local area." });

        _dashboardMock
            .Setup(d => d.LogDecisionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task AnalyzeLocalCompetitorsAsync_ValidInput_ReturnsAnalysis()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new MarketResearchAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.AnalyzeLocalCompetitorsAsync(Guid.NewGuid(), "Beauty Salon", "New York");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SuggestPricingStrategyAsync_ValidInput_ReturnsRecommendation()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new MarketResearchAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.SuggestPricingStrategyAsync(Guid.NewGuid(), "Haircut");

        result.Should().NotBeNullOrEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
