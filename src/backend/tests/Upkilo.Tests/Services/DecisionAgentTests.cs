using FluentAssertions;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class DecisionAgentTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<IAIDashboardService> _dashboardMock = new();

    public DecisionAgentTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Performance looks steady. Consider investing in marketing." });

        _dashboardMock
            .Setup(d => d.LogDecisionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task AnalyzePerformanceAsync_EmptyDb_ReturnsAnalysis()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new DecisionAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.AnalyzePerformanceAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetGrowthRecommendationsAsync_EmptyDb_ReturnsRecommendations()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new DecisionAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.GetGrowthRecommendationsAsync(Guid.NewGuid());

        result.Should().NotBeNullOrEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
