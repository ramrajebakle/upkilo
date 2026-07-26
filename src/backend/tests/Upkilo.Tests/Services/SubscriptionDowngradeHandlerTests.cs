using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class SubscriptionDowngradeHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<SubscriptionDowngradeHandler>> _loggerMock;

    public SubscriptionDowngradeHandlerTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<SubscriptionDowngradeHandler>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SubscriptionDowngradeHandler(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleDowngradeAsync_TenantWithNoExcessResources_CompletesWithoutThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Downgrade Test",
            Slug = "downgrade-test",
            SubscriptionTier = SubscriptionTier.Starter
        });
        ctx.SaveChanges();

        var svc = new SubscriptionDowngradeHandler(ctx, _loggerMock.Object);

        var act = async () => await svc.HandleDowngradeAsync(
            tenantId,
            oldPlanName: "Professional",
            newPlanName: "Starter",
            newMaxStaff: 10,
            newMaxLocations: 5,
            newMaxServices: 50,
            newWebhooks: false,
            newApiAccess: false);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleDowngradeAsync_ExcessStaff_DeactivatesExcessStaff()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Excess Biz", Slug = "excess-biz" });

        // Add 3 staff members (will exceed new plan limit of 1)
        for (int i = 0; i < 3; i++)
        {
            ctx.StaffMembers.Add(new StaffMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = $"Staff{i}",
                LastName = "Member",
                Email = $"staff{i}@example.com",
                IsActive = true
            });
        }
        ctx.SaveChanges();

        var svc = new SubscriptionDowngradeHandler(ctx, _loggerMock.Object);
        await svc.HandleDowngradeAsync(
            tenantId,
            oldPlanName: "Professional",
            newPlanName: "Free",
            newMaxStaff: 1,
            newMaxLocations: 1,
            newMaxServices: 10,
            newWebhooks: false,
            newApiAccess: false);

        var activeStaff = ctx.StaffMembers.Count(s => s.TenantId == tenantId && s.IsActive);
        activeStaff.Should().Be(1);
    }

    public void Dispose() => _dbFactory.Dispose();
}
