using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class SiemLoggingServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<SiemLoggingService>> _loggerMock = new();

    private SiemLoggingService CreateService(HttpClient? client = null)
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);
        _configMock.Setup(c => c["Siem:Endpoint"]).Returns(string.Empty);

        if (client == null)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });
            client = new HttpClient(handlerMock.Object);
        }

        return new SiemLoggingService(client, _configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ForwardEventAsync_ValidEvent_DoesNotThrow()
    {
        var svc = CreateService();

        var act = async () => await svc.ForwardEventAsync(
            "UserLogin",
            new { UserId = Guid.NewGuid(), IpAddress = "127.0.0.1" },
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForwardEventAsync_NoEndpointConfigured_StillCompletesGracefully()
    {
        var svc = CreateService();

        var act = async () => await svc.ForwardEventAsync("SuspiciousActivity", new { detail = "test" });

        await act.Should().NotThrowAsync();
    }
}
