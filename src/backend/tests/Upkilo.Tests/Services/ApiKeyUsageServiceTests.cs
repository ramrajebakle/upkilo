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

public class ApiKeyUsageServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<ApiKeyUsageService>> _loggerMock = new();

    public ApiKeyUsageServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private async Task<(ApiKeyUsageService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, ApiKey key)> Seed()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = "TestKey",
            KeyHash = "hash", Prefix = "sk_test"
        };
        ctx.ApiKeys.Add(apiKey);
        await ctx.SaveChangesAsync();
        return (new ApiKeyUsageService(ctx, _loggerMock.Object), ctx, apiKey);
    }

    [Fact]
    public async Task RecordUsageAsync_WhenKeyExists_UpdatesLastUsedAt()
    {
        var (sut, ctx, key) = await Seed();
        var before = DateTime.UtcNow.AddSeconds(-1);

        await sut.RecordUsageAsync(key.Id, "/api/bookings", 200, 120);

        ctx.ChangeTracker.Clear();
        var updated = ctx.ApiKeys.Find(key.Id);
        updated!.LastUsedAt.Should().NotBeNull();
        updated.LastUsedAt!.Value.Should().BeAfter(before);
    }

    [Fact]
    public async Task RecordUsageAsync_WhenKeyExists_CreatesAuditEntry()
    {
        var (sut, ctx, key) = await Seed();

        await sut.RecordUsageAsync(key.Id, "/api/bookings", 200, 120);

        ctx.ChangeTracker.Clear();
        ctx.AuditEntries.Should().HaveCount(1);
        ctx.AuditEntries.First().EntityType.Should().Be("ApiKey");
    }

    [Fact]
    public async Task RecordUsageAsync_WhenKeyNotFound_DoesNothing()
    {
        var (sut, ctx, _) = await Seed();

        // Should not throw
        await sut.RecordUsageAsync(Guid.NewGuid(), "/api/test", 404, 50);

        ctx.ChangeTracker.Clear();
        ctx.AuditEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsageStatsAsync_WhenKeyExists_ReturnsTotalRequests()
    {
        var (sut, _, key) = await Seed();

        await sut.RecordUsageAsync(key.Id, "/api/bookings", 200, 100);
        await sut.RecordUsageAsync(key.Id, "/api/clients", 200, 80);

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);
        var stats = await sut.GetUsageStatsAsync(key.Id, from, to);

        stats.TotalRequests.Should().Be(2);
        stats.ApiKeyId.Should().Be(key.Id);
    }

    [Fact]
    public async Task GetUsageStatsAsync_WhenKeyNotFound_ReturnsEmptyStats()
    {
        var (sut, _, _) = await Seed();

        var stats = await sut.GetUsageStatsAsync(Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        stats.TotalRequests.Should().Be(0);
    }
}
