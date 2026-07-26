using FluentAssertions;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.Agents;

namespace Upkilo.Tests.Services;

public class ImageGenerationAgentTests
{
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly ImageGenerationAgent _sut;

    public ImageGenerationAgentTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateImageAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, ImageUrl = "https://example.com/image.png" });

        _sut = new ImageGenerationAgent(_aiServiceMock.Object);
    }

    [Fact]
    public async Task GenerateMarketingImageAsync_ValidInput_ReturnsUrl()
    {
        var tenantId = Guid.NewGuid();

        var result = await _sut.GenerateMarketingImageAsync(tenantId, "My Salon", "Haircut", "modern");

        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("https://");
    }
}
