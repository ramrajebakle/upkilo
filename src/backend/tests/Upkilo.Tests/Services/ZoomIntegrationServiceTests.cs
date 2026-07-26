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

public class ZoomIntegrationServiceTests
{
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<ZoomIntegrationService>> _loggerMock = new();

    private (ZoomIntegrationService Service, Mock<HttpMessageHandler> Handler) CreateService(
        HttpStatusCode tokenStatus = HttpStatusCode.OK,
        HttpStatusCode meetingStatus = HttpStatusCode.Created,
        string meetingResponseBody = "{\"join_url\":\"https://zoom.us/j/12345\"}")
    {


        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call = token endpoint, second call = meeting endpoint
                return callCount == 1
                    ? new HttpResponseMessage(tokenStatus) { Content = new StringContent("{\"access_token\":\"tok123\"}", System.Text.Encoding.UTF8, "application/json") }
                    : new HttpResponseMessage(meetingStatus) { Content = new StringContent(meetingResponseBody, System.Text.Encoding.UTF8, "application/json") };
            });

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.zoom.us/v2/") };
        return (new ZoomIntegrationService(client, _configMock.Object, _loggerMock.Object), handlerMock);
    }

    [Fact]
    public async Task CreateMeetingAsync_HttpOk_ReturnsMeetingUrl()
    {
        _configMock.Setup(c => c["Zoom:AccountId"]).Returns("acc123");
        _configMock.Setup(c => c["Zoom:ClientId"]).Returns("cli123");
        _configMock.Setup(c => c["Zoom:ClientSecret"]).Returns("sec123");


        var (svc, _) = CreateService();

        var result = await svc.CreateMeetingAsync(Guid.NewGuid(), "Test Meeting", DateTime.UtcNow.AddHours(1), 60, "UTC");

        result.Should().Be("https://zoom.us/j/12345");
    }

    [Fact]
    public async Task CreateMeetingAsync_HttpError_ReturnsNull()
    {
        _configMock.Setup(c => c["Zoom:AccountId"]).Returns("acc123");
        _configMock.Setup(c => c["Zoom:ClientId"]).Returns("cli123");
        _configMock.Setup(c => c["Zoom:ClientSecret"]).Returns("sec123");


        var (svc, _) = CreateService(HttpStatusCode.OK, HttpStatusCode.BadRequest, "{}");

        var result = await svc.CreateMeetingAsync(Guid.NewGuid(), "Test Meeting", DateTime.UtcNow.AddHours(1), 60, "UTC");

        result.Should().BeNull();
    }
}
