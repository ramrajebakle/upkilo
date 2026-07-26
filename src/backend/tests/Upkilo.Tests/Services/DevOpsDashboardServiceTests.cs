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

public class DevOpsDashboardServiceTests
{
    private readonly Mock<ILogger<DevOpsDashboardService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private DevOpsDashboardService CreateService()
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new DevOpsDashboardService(_loggerMock.Object, _configMock.Object, _httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task TriggerRollbackAsync_NoAzureConfig_ThrowsInvalidOperation()
    {
        var svc = CreateService();

        Func<Task> act = async () => await svc.TriggerRollbackAsync("staging", "test rollback");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateCanaryWeightAsync_NoAzureConfig_ThrowsInvalidOperation()
    {
        var svc = CreateService();

        Func<Task> act = async () => await svc.UpdateCanaryWeightAsync("api", 10);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
