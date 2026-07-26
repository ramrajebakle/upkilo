using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.AI;

namespace Upkilo.Tests.Services;

public class AIIntentServiceTests
{
    private readonly Mock<ILogger<AIIntentService>> _loggerMock = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly AIIntentService _sut;

    public AIIntentServiceTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "BookAppointment" });

        _sut = new AIIntentService(_loggerMock.Object, _aiServiceMock.Object);
    }

    [Fact]
    public async Task ParseIntentAsync_ValidMessage_ReturnsIntentString()
    {
        var tenantId = Guid.NewGuid();

        var result = await _sut.ParseIntentAsync("I want to book an appointment", tenantId);

        result.Should().NotBeNullOrEmpty();
        new[] { "ModifyBooking", "BookAppointment", "InquirePricing", "InquireLocation", "UnknownIntent" }
            .Should().Contain(result);
    }

    [Fact]
    public async Task ParseIntentAsync_EmptyMessage_ReturnsUnknown()
    {
        var tenantId = Guid.NewGuid();
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = false, Error = "AI unavailable" });

        var result = await _sut.ParseIntentAsync("", tenantId);

        result.Should().Be("UnknownIntent");
    }
}
