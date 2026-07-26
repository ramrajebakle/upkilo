using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class OAuth2AppServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<OAuth2AppService>> _loggerMock;

    public OAuth2AppServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<OAuth2AppService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new OAuth2AppService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAppAsync_ValidInput_ReturnsAppWithSecret()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new OAuth2AppService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var (app, plainSecret) = await svc.RegisterAppAsync(
            tenantId,
            "My Test App",
            "A test OAuth2 app",
            new[] { "https://example.com/callback" },
            new[] { "read:bookings", "write:bookings" }
        );

        app.Should().NotBeNull();
        app.AppName.Should().Be("My Test App");
        app.TenantId.Should().Be(tenantId);
        app.ClientId.Should().NotBeNullOrEmpty();
        plainSecret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAppsForTenantAsync_NoApps_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new OAuth2AppService(ctx, _loggerMock.Object);

        var result = await svc.ListAppsAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateClientAsync_UnknownClientId_ReturnsNull()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new OAuth2AppService(ctx, _loggerMock.Object);

        var result = await svc.ValidateClientAsync("nonexistent-client-id", "some-secret");

        result.Should().BeNull();
    }

    public void Dispose() => _dbFactory.Dispose();
}
