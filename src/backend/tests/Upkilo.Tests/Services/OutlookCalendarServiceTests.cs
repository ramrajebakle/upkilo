using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class OutlookCalendarServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<OutlookCalendarService>> _loggerMock = new();
    private readonly IConfiguration _configuration;

    public OutlookCalendarServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("Authentication:Microsoft:ClientId", "test-client-id"),
                new System.Collections.Generic.KeyValuePair<string, string?>("Authentication:Microsoft:RedirectUri", "https://localhost/callback"),
            })
            .Build();
    }

    [Fact]
    public void GetAuthUrl_ReturnsNonEmptyUrl()
    {
        using var context = _dbFactory.CreateContext();
        var service = new OutlookCalendarService(context, _configuration, _loggerMock.Object);

        var url = service.GetAuthUrl("outlook", Guid.NewGuid());

        url.Should().NotBeNullOrEmpty();
        url.Should().Contain("microsoftonline.com");
    }

    [Fact]
    public async Task SyncBookingsAsync_NoToken_CompletesWithoutThrow()
    {
        using var context = _dbFactory.CreateContext();
        var service = new OutlookCalendarService(context, _configuration, _loggerMock.Object);

        var act = async () => await service.SyncBookingsAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
