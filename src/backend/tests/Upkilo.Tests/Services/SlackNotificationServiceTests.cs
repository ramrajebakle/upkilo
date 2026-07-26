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
using Xunit;

namespace Upkilo.Tests.Services;

public class SlackNotificationServiceTests
{
    private readonly Mock<ILogger<SlackNotificationService>> _loggerMock = new();

    private (SlackNotificationService Service, Mock<HttpMessageHandler> Handler) CreateService(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent("ok", System.Text.Encoding.UTF8, "text/plain") });

        var client = new HttpClient(handlerMock.Object);
        return (new SlackNotificationService(client, _loggerMock.Object), handlerMock);
    }

    [Fact]
    public async Task SendNotificationAsync_ValidWebhookUrl_ReturnsTrue()
    {
        var (svc, _) = CreateService(HttpStatusCode.OK);

        var result = await svc.SendNotificationAsync("https://hooks.slack.com/services/test/webhook", "Hello from tests!");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotificationAsync_HttpError_ReturnsFalse()
    {
        var (svc, _) = CreateService(HttpStatusCode.BadRequest);

        var result = await svc.SendNotificationAsync("https://hooks.slack.com/services/test/webhook", "Hello from tests!");

        result.Should().BeFalse();
    }
}
