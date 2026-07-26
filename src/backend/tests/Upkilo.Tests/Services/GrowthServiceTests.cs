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

public class GrowthServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public GrowthServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new GrowthService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchDirectoryAsync_NoListings_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        ctx.BusinessListings.RemoveRange(ctx.BusinessListings);
        await ctx.SaveChangesAsync();

        var svc = new GrowthService(ctx);

        var result = await svc.SearchDirectoryAsync(null, null);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReferralSummaryAsync_NoReferrals_ReturnsZeroStats()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new GrowthService(ctx);

        var result = await svc.GetReferralSummaryAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.TotalReferrals.Should().Be(0);
        result.TotalCreditsEarned.Should().Be(0);
    }

    [Fact]
    public async Task GenerateReferralCodeAsync_NewTenant_CreatesCode()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "growth-test" });
        ctx.SaveChanges();

        var svc = new GrowthService(ctx);
        var code = await svc.GenerateReferralCodeAsync(tenantId);

        code.Should().NotBeNullOrEmpty();
        code.Should().Contain("UPKILO");
    }

    public void Dispose() => _dbFactory.Dispose();
}
