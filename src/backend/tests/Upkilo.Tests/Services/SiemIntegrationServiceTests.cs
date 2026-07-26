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

public class SiemIntegrationServiceTests
{
    private readonly Mock<ILogger<SiemIntegrationService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private SiemIntegrationService CreateService()
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);
        _configMock.Setup(c => c["Security:SiemEndpoint"]).Returns(string.Empty);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new SiemIntegrationService(_loggerMock.Object, _httpClientFactoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task LogSecurityEventAsync_NoEndpointConfig_CompletesGracefully()
    {
        var svc = CreateService();
        var siemEvent = new SiemEvent
        {
            EventType = "LoginAttempt",
            TenantId = Guid.NewGuid(),
            Details = "Test login attempt"
        };

        var act = async () => await svc.LogSecurityEventAsync(siemEvent);

        await act.Should().NotThrowAsync();
    }
}
