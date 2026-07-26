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

public class SplitPaymentServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<SplitPaymentService>> _loggerMock = new();

    public SplitPaymentServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (SplitPaymentService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.SaveChanges();
        return (new SplitPaymentService(ctx, _loggerMock.Object), ctx, tenantId);
    }

    [Fact]
    public async Task CreateDepositAsync_CalculatesDepositAmountCorrectly()
    {
        var (sut, _, tenantId) = CreateSut();

        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);

        split.DepositAmount.Should().Be(100m);
        split.TotalAmount.Should().Be(200m);
        split.Status.Should().Be("Pending");
    }

    [Theory]
    [InlineData(100.0, 25.0, 25.0)]
    [InlineData(100.0, 50.0, 50.0)]
    [InlineData(100.0, 100.0, 100.0)]
    [InlineData(300.0, 33.33, 99.99)]
    public async Task CreateDepositAsync_VariousPercentages_CalculatesCorrectly(
        double total, double pct, double expectedDeposit)
    {
        var (sut, _, tenantId) = CreateSut();

        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), (decimal)total, (decimal)pct);

        split.DepositAmount.Should().Be((decimal)expectedDeposit);
    }

    [Fact]
    public async Task RecordDepositPaymentAsync_WhenSplitExists_UpdatesStatus()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);

        var result = await sut.RecordDepositPaymentAsync(split.Id, "pi_test_123");

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var updated = ctx.Set<SplitPayment>().Find(split.Id);
        updated!.Status.Should().Be("DepositPaid");
        updated.DepositPaidAt.Should().NotBeNull();
        updated.StripePaymentIntentId.Should().Be("pi_test_123");
    }

    [Fact]
    public async Task RecordDepositPaymentAsync_WhenNotFound_ReturnsFalse()
    {
        var (sut, _, _) = CreateSut();

        var result = await sut.RecordDepositPaymentAsync(Guid.NewGuid(), "pi_whatever");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RecordFullPaymentAsync_UpdatesStatusToFullyPaid()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);
        await sut.RecordDepositPaymentAsync(split.Id, "pi_123");

        await sut.RecordFullPaymentAsync(split.Id);

        ctx.ChangeTracker.Clear();
        ctx.Set<SplitPayment>().Find(split.Id)!.Status.Should().Be("FullyPaid");
    }

    [Fact]
    public async Task GetRemainingBalanceAsync_WhenPending_ReturnsTotalAmount()
    {
        var (sut, _, tenantId) = CreateSut();
        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);

        var remaining = await sut.GetRemainingBalanceAsync(split.Id);

        remaining.Should().Be(200m);
    }

    [Fact]
    public async Task GetRemainingBalanceAsync_WhenDepositPaid_ReturnsRemainingBalance()
    {
        var (sut, _, tenantId) = CreateSut();
        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);
        await sut.RecordDepositPaymentAsync(split.Id, "pi_123");

        var remaining = await sut.GetRemainingBalanceAsync(split.Id);

        remaining.Should().Be(100m); // 200 - 100 = 100
    }

    [Fact]
    public async Task GetRemainingBalanceAsync_WhenFullyPaid_ReturnsZero()
    {
        var (sut, _, tenantId) = CreateSut();
        var split = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);
        await sut.RecordFullPaymentAsync(split.Id);

        var remaining = await sut.GetRemainingBalanceAsync(split.Id);

        remaining.Should().Be(0m);
    }

    [Fact]
    public async Task GetPendingPaymentsAsync_ReturnsOnlyDepositPaidSplits()
    {
        var (sut, _, tenantId) = CreateSut();
        var split1 = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 100m, 50m);
        var split2 = await sut.CreateDepositAsync(tenantId, Guid.NewGuid(), 200m, 50m);

        await sut.RecordDepositPaymentAsync(split1.Id, "pi_a");
        // split2 stays "Pending"

        var pending = await sut.GetPendingPaymentsAsync(tenantId);

        pending.Should().HaveCount(1);
        pending.First().Id.Should().Be(split1.Id);
    }
}
