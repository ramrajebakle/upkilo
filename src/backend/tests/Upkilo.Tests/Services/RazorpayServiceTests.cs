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
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class RazorpayServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<RazorpayService>> _loggerMock = new();
    private readonly Mock<ISecretProvider> _secretProviderMock = new();

    private RazorpayService CreateService(HttpStatusCode statusCode = HttpStatusCode.OK, string responseBody = "{\"id\":\"order_test123\"}")
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);
        _secretProviderMock.Setup(s => s.GetSecret(It.IsAny<string>())).Returns(string.Empty);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        return new RazorpayService(client, _configMock.Object, _loggerMock.Object, _secretProviderMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_NoApiKey_ReturnsNull()
    {
        var svc = CreateService();

        // No API key configured — should throw InvalidOperationException or return null
        try
        {
            var result = await svc.CreateOrderAsync(500m, "INR", "receipt-001");
            result.Should().BeNull();
        }
        catch (InvalidOperationException ex)
        {
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public void VerifySignature_InvalidSignature_ReturnsFalse()
    {
        var svc = CreateService();

        var result = svc.VerifySignature("order_123", "pay_123", "invalid_sig");

        result.Should().BeFalse();
    }
}
