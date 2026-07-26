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

public class UpsellTriggerServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<UpsellTriggerService>> _loggerMock;

    public UpsellTriggerServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<UpsellTriggerService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new UpsellTriggerService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateTriggersAsync_UnknownTenant_ReturnsEmptyList()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new UpsellTriggerService(ctx, _loggerMock.Object);

        var result = await svc.EvaluateTriggersAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateTriggersAsync_NewTenantWithNoData_ReturnsEmptyOrDefaultTriggers()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "New Biz",
            Slug = "new-biz",
            SubscriptionTier = SubscriptionTier.Starter
        });
        ctx.SaveChanges();

        var svc = new UpsellTriggerService(ctx, _loggerMock.Object);
        var result = await svc.EvaluateTriggersAsync(tenantId);

        // With no plan limits to breach and no data, may return empty or staff-limit trigger
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateTriggersAsync_TrialEndingSoon_ReturnsCriticalTrigger()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Trial Biz",
            Slug = "trial-biz",
            SubscriptionTier = SubscriptionTier.Starter,
            TrialEndsAt = DateTime.UtcNow.AddDays(1) // Trial ending in 1 day
        });
        ctx.SaveChanges();

        var svc = new UpsellTriggerService(ctx, _loggerMock.Object);
        var result = await svc.EvaluateTriggersAsync(tenantId);

        result.Should().NotBeNull();
        result.Should().Contain(t => t.Type == "trial_ending");
    }

    public void Dispose() => _dbFactory.Dispose();
}
