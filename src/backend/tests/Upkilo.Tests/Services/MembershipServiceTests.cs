using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class MembershipServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<MembershipService>> _loggerMock = new();

    public MembershipServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (MembershipService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.SaveChanges();
        return (new MembershipService(ctx, _loggerMock.Object), ctx, tenantId);
    }

    private async Task<MembershipPlan> SeedPlan(MembershipService sut, Guid tenantId, string billing = "monthly")
    {
        return await sut.CreatePlanAsync(tenantId, new MembershipPlan
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = "Gold", Price = 29.99m,
            BillingInterval = billing, ServicesIncluded = 5, IsActive = true
        });
    }

    private async Task<Client> SeedClient(Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId)
    {
        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Jane", Email = "j@t.com" };
        ctx.Clients.Add(client);
        await ctx.SaveChangesAsync();
        return client;
    }

    [Fact]
    public async Task CreatePlanAsync_PersistsPlan()
    {
        var (sut, ctx, tenantId) = CreateSut();

        var plan = await SeedPlan(sut, tenantId);

        plan.Id.Should().NotBeEmpty();
        ctx.ChangeTracker.Clear();
        ctx.MembershipPlans.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPlansAsync_ReturnsOnlyTenantPlans()
    {
        var (sut, _, tenantId) = CreateSut();
        await SeedPlan(sut, tenantId);
        await SeedPlan(sut, Guid.NewGuid()); // Other tenant

        var plans = await sut.GetPlansAsync(tenantId);

        plans.Should().HaveCount(1);
    }

    [Fact]
    public async Task SubscribeClientAsync_CreatesMembership()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var plan = await SeedPlan(sut, tenantId);
        var client = await SeedClient(ctx, tenantId);

        var membership = await sut.SubscribeClientAsync(tenantId, client.Id, plan.Id);

        membership.Status.Should().Be(MembershipStatus.Active);
        membership.ClientId.Should().Be(client.Id);
    }

    [Fact]
    public async Task SubscribeClientAsync_WhenPlanNotFound_ThrowsKeyNotFoundException()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var client = await SeedClient(ctx, tenantId);

        var act = () => sut.SubscribeClientAsync(tenantId, client.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ImmediateCancel_SetsStatusCancelled()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var plan = await SeedPlan(sut, tenantId);
        var client = await SeedClient(ctx, tenantId);
        var sub = await sut.SubscribeClientAsync(tenantId, client.Id, plan.Id);

        var result = await sut.CancelSubscriptionAsync(sub.Id, tenantId, immediately: true);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.ClientMemberships.Find(sub.Id)!.Status.Should().Be(MembershipStatus.Cancelled);
    }

    [Fact]
    public async Task PauseSubscriptionAsync_SetsMembershipToPaused()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var plan = await SeedPlan(sut, tenantId);
        var client = await SeedClient(ctx, tenantId);
        var sub = await sut.SubscribeClientAsync(tenantId, client.Id, plan.Id);

        await sut.PauseSubscriptionAsync(sub.Id, tenantId, null);

        ctx.ChangeTracker.Clear();
        ctx.ClientMemberships.Find(sub.Id)!.Status.Should().Be(MembershipStatus.Paused);
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_RestoresMembershipToActive()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var plan = await SeedPlan(sut, tenantId);
        var client = await SeedClient(ctx, tenantId);
        var sub = await sut.SubscribeClientAsync(tenantId, client.Id, plan.Id);
        await sut.PauseSubscriptionAsync(sub.Id, tenantId, null);

        await sut.ResumeSubscriptionAsync(sub.Id, tenantId);

        ctx.ChangeTracker.Clear();
        ctx.ClientMemberships.Find(sub.Id)!.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task RecordUsageAsync_WhenUnderLimit_IncrementsUsage()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var plan = await SeedPlan(sut, tenantId); // 5 services included
        var client = await SeedClient(ctx, tenantId);
        var sub = await sut.SubscribeClientAsync(tenantId, client.Id, plan.Id);

        var result = await sut.RecordUsageAsync(sub.Id, tenantId, Guid.NewGuid());

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.ClientMemberships.Find(sub.Id)!.ServicesUsedThisPeriod.Should().Be(1);
    }

    [Fact]
    public async Task RecordUsageAsync_WhenAtLimit_ReturnsFalse()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var plan = await SeedPlan(sut, tenantId); // 5 included
        var client = await SeedClient(ctx, tenantId);
        var sub = await sut.SubscribeClientAsync(tenantId, client.Id, plan.Id);

        // Use all 5
        for (int i = 0; i < 5; i++)
            await sut.RecordUsageAsync(sub.Id, tenantId, Guid.NewGuid());

        var result = await sut.RecordUsageAsync(sub.Id, tenantId, Guid.NewGuid());

        result.Should().BeFalse();
    }
}
