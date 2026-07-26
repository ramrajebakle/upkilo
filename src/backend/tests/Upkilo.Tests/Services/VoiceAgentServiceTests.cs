using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class VoiceAgentServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<ISmsService> _smsMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<VoiceAgentService>> _loggerMock = new();
    private readonly VoiceAgentService _sut;

    public VoiceAgentServiceTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Sure! I can help you book an appointment." });

        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _sut = new VoiceAgentService(
            _aiServiceMock.Object,
            _smsMock.Object,
            _dbFactory.CreateContext(),
            _cacheMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task ProcessVoiceRequestAsync_ValidSpeech_ReturnsTextResponse()
    {
        var tenantId = Guid.NewGuid();
        var callSid = Guid.NewGuid().ToString("N");

        var result = await _sut.ProcessVoiceRequestAsync(tenantId, "I want to book a haircut for tomorrow", callSid);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessVoiceRequestAsync_EmptySpeech_ReturnsGreetingPrompt()
    {
        var tenantId = Guid.NewGuid();
        var callSid = Guid.NewGuid().ToString("N");

        var result = await _sut.ProcessVoiceRequestAsync(tenantId, "", callSid);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Hello");
    }
}
