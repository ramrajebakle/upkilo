using FluentAssertions;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class ChatbotServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly Mock<IAIDashboardService> _dashboardMock = new();
    private readonly Mock<IBookingService> _bookingMock = new();
    private readonly Mock<ISchedulingService> _schedulingMock = new();

    public ChatbotServiceTests()
    {
        _aiServiceMock
            .Setup(a => a.GenerateTextAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AIGenerationResult { Success = true, Content = "Hello! How can I help you today?" });

        _dashboardMock
            .Setup(d => d.LogDecisionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private ChatbotService CreateSut() => new ChatbotService(
        _dbFactory.CreateContext(),
        _aiServiceMock.Object,
        _dashboardMock.Object,
        _bookingMock.Object,
        _schedulingMock.Object);

    [Fact]
    public async Task ProcessMessageAsync_GreetingMessage_ReturnsResponse()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var request = new ChatRequestDto
        {
            TenantId = tenantId,
            Message = "Hello!",
            ExternalId = "user-123",
            Channel = ConversationChannel.WebChat
        };

        var result = await sut.ProcessMessageAsync(request);

        result.Should().NotBeNull();
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_BookingMessage_ReturnsNonNullResponse()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var request = new ChatRequestDto
        {
            TenantId = tenantId,
            Message = "I want to book an appointment",
            ExternalId = "user-456",
            Channel = ConversationChannel.WebChat
        };

        var result = await sut.ProcessMessageAsync(request);

        result.Should().NotBeNull();
    }

    public void Dispose() => _dbFactory.Dispose();
}
