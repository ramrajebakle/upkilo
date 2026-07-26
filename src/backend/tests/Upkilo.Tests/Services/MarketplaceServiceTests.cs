using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class MarketplaceServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public MarketplaceServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new MarketplaceService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFeaturedListingsAsync_NoListings_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        ctx.BusinessListings.RemoveRange(ctx.BusinessListings);
        await ctx.SaveChangesAsync();

        var svc = new MarketplaceService(ctx);

        var result = await svc.GetFeaturedListingsAsync(null, null, null);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFeaturedListingsAsync_WithFeaturedListings_ReturnsOnlyFeatured()
    {
        using var ctx = _dbFactory.CreateContext();
        ctx.BusinessListings.RemoveRange(ctx.BusinessListings);
        await ctx.SaveChangesAsync();

        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Spa", Slug = "test-spa" });

        ctx.BusinessListings.Add(new BusinessListing
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BusinessName = "Featured Spa",
            Category = "Beauty",
            Slug = "featured-spa",
            IsFeatured = true,
            IsActive = true
        });

        ctx.BusinessListings.Add(new BusinessListing
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BusinessName = "Regular Spa",
            Category = "Beauty",
            Slug = "regular-spa",
            IsFeatured = false,
            IsActive = true
        });
        ctx.SaveChanges();

        var svc = new MarketplaceService(ctx);
        var result = await svc.GetFeaturedListingsAsync(null, null, null);

        result.Should().HaveCount(1);
        result.First().BusinessName.Should().Be("Featured Spa");
    }

    [Fact]
    public async Task CalculateLeadFeesAsync_NoLeads_ReturnsZero()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new MarketplaceService(ctx);

        var result = await svc.CalculateLeadFeesAsync(Guid.NewGuid());

        result.Should().Be(0m);
    }

    public void Dispose() => _dbFactory.Dispose();
}
