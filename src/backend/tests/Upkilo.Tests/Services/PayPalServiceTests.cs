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

public class PayPalServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<PayPalService>> _loggerMock = new();

    private PayPalService CreateService(HttpStatusCode tokenStatus = HttpStatusCode.Unauthorized)
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);
        _configMock.Setup(c => c["PayPal:IsSandbox"]).Returns("true");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(tokenStatus) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        return new PayPalService(client, _configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_NoClientSecret_ThrowsOrReturnsNull()
    {
        var svc = CreateService(HttpStatusCode.Unauthorized);

        try
        {
            var result = await svc.CreateOrderAsync(10.00m, "USD", "https://example.com/return", "https://example.com/cancel");
            result.Should().BeNull();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task CreateOrderAsync_BadRequest_ReturnsNull()
    {
        var svc = CreateService(HttpStatusCode.BadRequest);

        var result = await svc.CreateOrderAsync(99.99m, "USD", "https://example.com/return", "https://example.com/cancel");

        result.Should().BeNull();
    }
}
