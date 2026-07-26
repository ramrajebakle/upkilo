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

public class PagerDutyServiceTests
{
    private readonly Mock<ILogger<PagerDutyService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private PagerDutyService CreateService(string integrationKey = "")
    {
        _configMock.Setup(c => c["PagerDuty:IntegrationKey"]).Returns(integrationKey);
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new PagerDutyService(_loggerMock.Object, _httpClientFactoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task TriggerAlertAsync_NoIntegrationKey_CompletesGracefully()
    {
        var svc = CreateService(integrationKey: "");

        var act = async () => await svc.TriggerAlertAsync("Test summary", "error", "test-source", null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TriggerAlertAsync_ValidKey_SendsHttpPost()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{\"status\":\"success\"}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        _configMock.Setup(c => c["PagerDuty:IntegrationKey"]).Returns("test-key-12345");

        var svc = new PagerDutyService(_loggerMock.Object, _httpClientFactoryMock.Object, _configMock.Object);

        var act = async () => await svc.TriggerAlertAsync("Alert", "critical", "unit-test", new { info = "test" });

        await act.Should().NotThrowAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
