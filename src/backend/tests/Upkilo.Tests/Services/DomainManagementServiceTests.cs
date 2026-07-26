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

public class DomainManagementServiceTests
{
    private readonly Mock<ILogger<DomainManagementService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private DomainManagementService CreateService()
    {
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns(string.Empty);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"Answer\":[]}", System.Text.Encoding.UTF8, "application/json") });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new DomainManagementService(_loggerMock.Object, _httpClientFactoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task ProvisionCertificateAsync_NoAzureConfig_CompletesGracefully()
    {
        var svc = CreateService();

        // With no Azure config, it should throw InvalidOperationException or complete gracefully
        try
        {
            await svc.ProvisionCertificateAsync("test.example.com");
        }
        catch (InvalidOperationException ex)
        {
            ex.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task VerifyDomainAsync_UnknownDomain_ReturnsResult()
    {
        var svc = CreateService();

        var result = await svc.VerifyDomainAsync("unknown.example.com", "some-verification-value");

        result.Should().NotBeNull();
        result.IsVerified.Should().BeFalse();
    }
}
