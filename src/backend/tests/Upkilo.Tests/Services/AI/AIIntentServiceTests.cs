using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.AI;
using Xunit;

namespace Upkilo.Tests.Services.AI;

public class AIIntentServiceTests
{
    private readonly Mock<ILogger<AIIntentService>> _loggerMock = new();
    private readonly Mock<IAIService> _aiServiceMock = new();

    private AIIntentService CreateSut() =>
        new AIIntentService(_loggerMock.Object, _aiServiceMock.Object);

    [Theory]
    [InlineData("I want to cancel my appointment", "ModifyBooking")]
    [InlineData("Please reschedule my booking", "ModifyBooking")]
    [InlineData("I want to book a haircut", "BookAppointment")]
    [InlineData("how much does it cost?", "InquirePricing")]
    [InlineData("where is your salon located?", "InquireLocation")]
    [InlineData("random gibberish text", "UnknownIntent")]
    public async Task ParseIntentAsync_Heuristics_WhenAIFails_ClassifiesCorrectly(string message, string expectedIntent)
    {
        // AI fails → falls through to heuristic classifier
        _aiServiceMock.Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), null, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("AI unavailable"));

        var sut = CreateSut();

        var result = await sut.ParseIntentAsync(message, Guid.NewGuid());

        result.Should().Be(expectedIntent);
    }

    [Fact]
    public async Task ParseIntentAsync_WhenAIReturnsValidIntent_ReturnsThatIntent()
    {
        var aiResponse = new AIGenerationResult { Success = true, Content = "InquirePricing" };
        _aiServiceMock.Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), null, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        var sut = CreateSut();
        var result = await sut.ParseIntentAsync("what's your rate?", Guid.NewGuid());

        result.Should().Be("InquirePricing");
    }

    [Fact]
    public async Task ParseIntentAsync_WhenAIReturnsInvalidIntent_FallsBackToHeuristic()
    {
        // AI returns something not in the valid set
        var aiResponse = new AIGenerationResult { Success = true, Content = "BuyProduct" };
        _aiServiceMock.Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), null, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(aiResponse);

        var sut = CreateSut();
        var result = await sut.ParseIntentAsync("I want to book an appointment", Guid.NewGuid());

        result.Should().Be("BookAppointment"); // Heuristic fallback
    }
}
