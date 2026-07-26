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

public class LandingPageServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public LandingPageServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new LandingPageService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePageAsync_ValidInput_ReturnsPage()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var svc = new LandingPageService(ctx);

        var result = await svc.CreatePageAsync(tenantId, "Summer Sale", "summer-sale", "<h1>Summer Sale</h1>", null);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Title.Should().Be("Summer Sale");
        result.TenantId.Should().Be(tenantId);
        result.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task GetPageBySlugAsync_UnknownSlug_ReturnsNull()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new LandingPageService(ctx);

        var result = await svc.GetPageBySlugAsync("nonexistent-slug");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPagesAsync_NoPages_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new LandingPageService(ctx);

        var result = await svc.GetPagesAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
