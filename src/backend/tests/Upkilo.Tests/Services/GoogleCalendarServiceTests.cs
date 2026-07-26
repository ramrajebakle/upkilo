using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class GoogleCalendarServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<GoogleCalendarService>> _loggerMock = new();
    private readonly Mock<ISecretProvider> _secretProviderMock = new();
    private readonly IConfiguration _configuration;

    public GoogleCalendarServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("Authentication:Google:ClientId", "test-google-client"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Authentication:Google:ClientSecret", "test-google-secret"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Authentication:Google:RedirectUri", "https://localhost/callback"),
            })
            .Build();

        _secretProviderMock.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);
    }

    [Fact]
    public void GetAuthUrl_ReturnsNonEmptyString()
    {
        using var context = _dbFactory.CreateContext();
        var service = new GoogleCalendarService(_configuration, _loggerMock.Object, _secretProviderMock.Object, context);

        var url = service.GetAuthUrl("google", Guid.NewGuid());

        url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SyncBookingsAsync_NoToken_CompletesWithoutThrow()
    {
        using var context = _dbFactory.CreateContext();
        var service = new GoogleCalendarService(_configuration, _loggerMock.Object, _secretProviderMock.Object, context);

        var act = async () => await service.SyncBookingsAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
