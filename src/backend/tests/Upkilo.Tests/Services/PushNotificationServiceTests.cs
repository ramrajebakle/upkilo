using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class PushNotificationServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<PushNotificationService>> _loggerMock = new();
    private readonly Mock<ISecretProvider> _secretProviderMock = new();
    private readonly IConfiguration _configuration;

    public PushNotificationServiceTests()
    {
        _configuration = new ConfigurationBuilder().Build();
        _secretProviderMock.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);
        _secretProviderMock.Setup(s => s.GetSecretAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
    }

    [Fact]
    public async Task SendBrowserPushAsync_NoDeviceTokens_CompletesWithoutThrow()
    {
        using var context = _dbFactory.CreateContext();
        var service = new PushNotificationService(context, _loggerMock.Object, _configuration, _secretProviderMock.Object);

        var act = async () => await service.SendBrowserPushAsync(Guid.NewGuid(), "Test Title", "Test Body");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPushAsync_UnknownUser_CompletesWithoutThrow()
    {
        using var context = _dbFactory.CreateContext();
        var service = new PushNotificationService(context, _loggerMock.Object, _configuration, _secretProviderMock.Object);

        var act = async () => await service.SendBrowserPushAsync(Guid.NewGuid(), "Hello", "World", "https://example.com");

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
