using FluentAssertions;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class ChurnPredictorAgentTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<IAIDashboardService> _dashboardMock = new();

    public ChurnPredictorAgentTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Low churn risk based on recent activity." });

        _dashboardMock
            .Setup(d => d.LogDecisionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task PredictChurnRiskAsync_UnknownClient_ReturnsNotFound()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new ChurnPredictorAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.PredictChurnRiskAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().Be("Client not found.");
    }

    [Fact]
    public async Task PredictChurnRiskAsync_ExistingClient_ReturnsAnalysis()
    {
        using var context = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com"
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var sut = new ChurnPredictorAgent(_aiServiceMock.Object, context, _dashboardMock.Object);

        var result = await sut.PredictChurnRiskAsync(tenantId, client.Id);

        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("Client not found.");
    }

    public void Dispose() => _dbFactory.Dispose();
}
