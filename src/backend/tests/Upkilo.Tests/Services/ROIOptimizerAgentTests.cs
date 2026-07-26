using FluentAssertions;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class ROIOptimizerAgentTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<IAIDashboardService> _dashboardMock = new();

    public ROIOptimizerAgentTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "ROI is 200%. Increase Facebook ad spend." });

        _dashboardMock
            .Setup(d => d.LogDecisionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task AnalyzeCampaignROIAsync_UnknownCampaign_ReturnsNotFound()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new ROIOptimizerAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.AnalyzeCampaignROIAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().Be("Campaign not found.");
    }

    [Fact]
    public async Task SuggestBudgetAllocationAsync_EmptyDb_ReturnsRecommendations()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new ROIOptimizerAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.SuggestBudgetAllocationAsync(Guid.NewGuid(), 1000m);

        result.Should().NotBeNullOrEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
