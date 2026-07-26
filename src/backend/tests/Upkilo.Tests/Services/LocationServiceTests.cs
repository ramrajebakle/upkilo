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

public class LocationServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<LocationService>> _loggerMock = new();

    public LocationServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (LocationService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.SaveChanges();
        return (new LocationService(ctx, _loggerMock.Object), ctx, tenantId);
    }

    [Fact]
    public async Task CreateAsync_FirstLocation_IsAutomaticallyPrimary()
    {
        var (sut, ctx, tenantId) = CreateSut();

        var loc = await sut.CreateAsync(tenantId, new Location { Name = "Main Branch" });

        loc.IsPrimary.Should().BeTrue();
        loc.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task CreateAsync_SecondLocation_IsNotPrimary()
    {
        var (sut, _, tenantId) = CreateSut();

        await sut.CreateAsync(tenantId, new Location { Name = "Branch 1" });
        var second = await sut.CreateAsync(tenantId, new Location { Name = "Branch 2" });

        second.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTenantLocations()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var otherTenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = otherTenantId, Name = "Other", Slug = "other" });
        ctx.SaveChanges();

        await sut.CreateAsync(tenantId, new Location { Name = "Mine" });
        await sut.CreateAsync(otherTenantId, new Location { Name = "Theirs" });

        var locations = await sut.GetAllAsync(tenantId);

        locations.Should().HaveCount(1);
        locations.First().Name.Should().Be("Mine");
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesFields()
    {
        var (sut, _, tenantId) = CreateSut();
        var loc = await sut.CreateAsync(tenantId, new Location { Name = "Old Name" });

        var updated = await sut.UpdateAsync(loc.Id, tenantId, new Location { Name = "New Name", IsActive = true });

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        var (sut, _, tenantId) = CreateSut();

        var result = await sut.UpdateAsync(Guid.NewGuid(), tenantId, new Location { Name = "Ghost" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesLocation()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var loc = await sut.CreateAsync(tenantId, new Location { Name = "ToDelete" });

        var result = await sut.DeleteAsync(loc.Id, tenantId);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.Set<Location>().Should().BeEmpty();
    }

    [Fact]
    public async Task SetDefaultAsync_ChangesOnlySpecifiedLocationToPrimary()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var loc1 = await sut.CreateAsync(tenantId, new Location { Name = "Branch 1" });
        var loc2 = await sut.CreateAsync(tenantId, new Location { Name = "Branch 2" });

        await sut.SetDefaultAsync(loc2.Id, tenantId);

        ctx.ChangeTracker.Clear();
        var locations = ctx.Set<Location>().Where(l => l.TenantId == tenantId).ToList();
        locations.First(l => l.Id == loc2.Id).IsPrimary.Should().BeTrue();
        locations.First(l => l.Id == loc1.Id).IsPrimary.Should().BeFalse();
    }
}
