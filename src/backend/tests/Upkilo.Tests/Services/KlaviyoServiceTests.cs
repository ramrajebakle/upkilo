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
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class KlaviyoServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<KlaviyoService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private KlaviyoService CreateService()
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new KlaviyoService(_loggerMock.Object, _dbFactory.CreateContext(), _httpClientFactoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task SyncContactsAsync_NoClients_CompletesWithoutThrow()
    {
        var svc = CreateService();
        var tenantId = Guid.NewGuid();

        var act = async () => await svc.SyncContactsAsync(tenantId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TrackEventAsync_ValidEvent_DoesNotThrow()
    {
        var svc = CreateService();
        var tenantId = Guid.NewGuid();

        var act = async () => await svc.TrackEventAsync(tenantId, "test@example.com", "TestEvent", new { property = "value" });

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
