using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class LinkedInAdsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<LinkedInAdsService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

    private LinkedInAdsService CreateService(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new LinkedInAdsService(_httpClientFactoryMock.Object, _dbFactory.CreateContext(), _loggerMock.Object);
    }

    [Fact]
    public async Task ConnectAccountAsync_ValidCode_ReturnsTrue()
    {
        var svc = CreateService(HttpStatusCode.OK);
        var tenantId = Guid.NewGuid();

        var result = await svc.ConnectAccountAsync(tenantId, "auth-code-12345");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetCampaignsAsync_NoCampaigns_ReturnsEmpty()
    {
        var svc = CreateService(HttpStatusCode.OK);
        var tenantId = Guid.NewGuid();

        var result = await svc.GetCampaignsAsync(tenantId);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
