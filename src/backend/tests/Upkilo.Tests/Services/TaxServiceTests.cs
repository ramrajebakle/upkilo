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

public class TaxServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    public TaxServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private async Task SeedTenantAsync(Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId)
    {
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t-" + tenantId.ToString()[..8] });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateTaxRateAsync_PersistsRate()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(ctx, tenantId);

        var sut = new TaxService(ctx);
        var rate = new TaxRate { Id = Guid.NewGuid(), Name = "GST", Percentage = 18m, IsActive = true };

        var result = await sut.CreateTaxRateAsync(tenantId, rate);

        result.TenantId.Should().Be(tenantId);
        ctx.ChangeTracker.Clear();
        ctx.TaxRates.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTaxRatesAsync_ReturnsOnlyActiveByDefault()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(ctx, tenantId);

        var sut = new TaxService(ctx);
        await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = Guid.NewGuid(), Name = "GST", Percentage = 18m, IsActive = true });
        await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = Guid.NewGuid(), Name = "OLD", Percentage = 10m, IsActive = false });

        var rates = await sut.GetTaxRatesAsync(tenantId, onlyActive: true);

        rates.Should().HaveCount(1);
        rates.First().Name.Should().Be("GST");
    }

    [Fact]
    public async Task GetTaxRatesAsync_WhenOnlyActiveFalse_ReturnsAll()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(ctx, tenantId);

        var sut = new TaxService(ctx);
        await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = Guid.NewGuid(), Name = "GST", Percentage = 18m, IsActive = true });
        await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = Guid.NewGuid(), Name = "OLD", Percentage = 10m, IsActive = false });

        var rates = await sut.GetTaxRatesAsync(tenantId, onlyActive: false);

        rates.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateTaxRateAsync_WhenIsDefault_ClearsExistingDefault()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(ctx, tenantId);

        var sut = new TaxService(ctx);
        var first = await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = Guid.NewGuid(), Name = "GST", Percentage = 18m, IsDefault = true, IsActive = true });
        var second = await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = Guid.NewGuid(), Name = "VAT", Percentage = 20m, IsDefault = true, IsActive = true });

        ctx.ChangeTracker.Clear();
        var defaultRate = await sut.GetDefaultTaxRateAsync(tenantId);
        defaultRate.Should().NotBeNull();
        defaultRate!.Name.Should().Be("VAT");
    }

    [Fact]
    public async Task DeleteTaxRateAsync_WhenExists_RemovesAndReturnsTrue()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(ctx, tenantId);

        var sut = new TaxService(ctx);
        var id = Guid.NewGuid();

        await sut.CreateTaxRateAsync(tenantId, new TaxRate { Id = id, Name = "GST", Percentage = 18m, IsActive = true });

        var deleted = await sut.DeleteTaxRateAsync(tenantId, id);

        deleted.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.TaxRates.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTaxRateAsync_WhenNotFound_ReturnsFalse()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new TaxService(ctx);

        var result = await sut.DeleteTaxRateAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }
}
