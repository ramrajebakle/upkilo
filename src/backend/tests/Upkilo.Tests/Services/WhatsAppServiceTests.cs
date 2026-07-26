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

public class WhatsAppServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<WhatsAppService>> _loggerMock = new();
    private readonly Mock<ISecretProvider> _secretProviderMock = new();
    private readonly IConfiguration _configuration;

    public WhatsAppServiceTests()
    {
        _configuration = new ConfigurationBuilder().Build();
        _secretProviderMock.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);
    }

    [Fact]
    public async Task SendWhatsAppAsync_NoApiKey_ReturnsFailureGracefully()
    {
        using var context = _dbFactory.CreateContext();
        var service = new WhatsAppService(_configuration, _loggerMock.Object, context, _secretProviderMock.Object);

        var result = await service.SendWhatsAppAsync(Guid.NewGuid(), "+1234567890", "Hello");

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendWhatsAppAsync_DisabledService_ReturnsErrorMessage()
    {
        using var context = _dbFactory.CreateContext();
        var service = new WhatsAppService(_configuration, _loggerMock.Object, context, _secretProviderMock.Object);

        var result = await service.SendWhatsAppAsync(Guid.NewGuid(), "+9999999999", "Test message");

        result.Error.Should().NotBeNullOrEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
