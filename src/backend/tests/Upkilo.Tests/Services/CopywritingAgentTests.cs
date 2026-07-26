using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Agents;

namespace Upkilo.Tests.Services;

public class CopywritingAgentTests
{
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<ILogger<CopywritingAgent>> _loggerMock = new();
    private readonly CopywritingAgent _sut;

    public CopywritingAgentTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Generated marketing content here." });

        _sut = new CopywritingAgent(_aiServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateEmailContentAsync_ValidInput_ReturnsContent()
    {
        var tenantId = Guid.NewGuid();

        var result = await _sut.GenerateEmailContentAsync(tenantId, "My Salon", "Haircut", "Young professionals", "Increase bookings");

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSmsContentAsync_ValidInput_ReturnsContent()
    {
        var tenantId = Guid.NewGuid();

        var result = await _sut.GenerateSmsContentAsync(tenantId, "My Salon", "Haircut", "Promote weekend offer");

        result.Should().NotBeNullOrEmpty();
    }
}
