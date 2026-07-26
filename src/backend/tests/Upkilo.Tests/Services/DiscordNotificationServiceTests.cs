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

public class DiscordNotificationServiceTests
{
    private readonly Mock<ILogger<DiscordNotificationService>> _loggerMock = new();

    private DiscordNotificationService CreateService(HttpStatusCode statusCode = HttpStatusCode.NoContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(string.Empty) });

        var client = new HttpClient(handlerMock.Object);
        return new DiscordNotificationService(client, _loggerMock.Object);
    }

    [Fact]
    public async Task SendNotificationAsync_ValidUrl_ReturnsTrue()
    {
        // Discord webhooks return 204 No Content on success
        var svc = CreateService(HttpStatusCode.NoContent);

        var result = await svc.SendNotificationAsync("https://discord.com/api/webhooks/123/abc", "Hello from tests!");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotificationAsync_HttpError_ReturnsFalse()
    {
        var svc = CreateService(HttpStatusCode.BadRequest);

        var result = await svc.SendNotificationAsync("https://discord.com/api/webhooks/123/abc", "Hello from tests!");

        result.Should().BeFalse();
    }
}
