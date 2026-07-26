using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class SmsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public SmsServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    public void Dispose() => _dbFactory.Dispose();

    private SmsService CreateSut()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var logger = new Mock<ILogger<SmsService>>();
        var context = _dbFactory.CreateContext();
        var secretProvider = new Mock<ISecretProvider>();
        return new SmsService(config, logger.Object, context, secretProvider.Object);
    }

    [Fact]
    public async Task SendSmsAsync_WhenNotConfigured_ReturnsDisabledResult()
    {
        var sut = CreateSut();
        var result = await sut.SendSmsAsync(Guid.NewGuid(), "+1234567890", "Test message");
        result.Success.Should().BeFalse(); // It will be false because CreateSut provides no config
        result.Error.Should().Be("SMS Service is disabled");
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_ReturnsDisabledResult()
    {
        var sut = CreateSut();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };
        // Note: This might fail on FindAsync if DB is empty, but we are testing the service logic
        var result = await sut.SendBookingConfirmationAsync(booking);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendVerificationCodeAsync_ReturnsDisabledResult()
    {
        var sut = CreateSut();
        var result = await sut.SendVerificationCodeAsync(Guid.NewGuid(), "+1234567890", "123456");
        result.Success.Should().BeFalse();
    }
}
