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

public class GdprAutomationServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<GdprAutomationService>> _loggerMock = new();

    public GdprAutomationServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (GdprAutomationService sut, Upkilo.Infrastructure.Data.AppDbContext ctx) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        return (new GdprAutomationService(ctx, _loggerMock.Object), ctx);
    }

    [Fact]
    public async Task AnonymizeClientDataAsync_WhenClientExists_ScrubbsPii()
    {
        var (sut, ctx) = CreateSut();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t" };
        ctx.Tenants.Add(tenant);
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PhoneNumber = "1234567890"
        };
        ctx.Clients.Add(client);
        await ctx.SaveChangesAsync();

        var result = await sut.AnonymizeClientDataAsync(client.Id);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var updated = ctx.Clients.First(c => c.Id == client.Id);
        updated.FirstName.Should().Be("Anonymized");
        updated.Email.Should().Contain("@upkilo.internal");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AnonymizeClientDataAsync_WhenClientNotFound_ReturnsFalse()
    {
        var (sut, _) = CreateSut();

        var result = await sut.AnonymizeClientDataAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnonymizeTenantDataAsync_AnonymizesAllClientsForTenant()
    {
        var (sut, ctx) = CreateSut();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "T", Slug = "t" };
        ctx.Tenants.Add(tenant);
        ctx.Clients.AddRange(
            new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "A", Email = "a@t.com" },
            new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "B", Email = "b@t.com" }
        );
        await ctx.SaveChangesAsync();

        var result = await sut.AnonymizeTenantDataAsync(tenantId);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.Clients.Where(c => c.TenantId == tenantId).All(c => c.FirstName == "Anonymized").Should().BeTrue();
    }

    [Fact]
    public async Task ExportClientDataAsync_WhenClientExists_ReturnsNonEmptyJson()
    {
        var (sut, ctx) = CreateSut();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t" };
        ctx.Tenants.Add(tenant);
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FirstName = "Export",
            LastName = "User",
            Email = "export@test.com"
        };
        ctx.Clients.Add(client);
        await ctx.SaveChangesAsync();

        var json = await sut.ExportClientDataAsync(client.Id);

        json.Should().Contain("Export");
    }
}
