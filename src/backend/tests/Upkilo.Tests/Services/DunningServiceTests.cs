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

public class DunningServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<DunningService>> _loggerMock = new();

    public DunningServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task ProcessDunningCyclesAsync_WhenNoCyclesDue_DoesNothing()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DunningService(ctx, _loggerMock.Object);

        // All cycles have future NextAttemptAt, so none should be processed
        ctx.DunningCycles.Add(new DunningCycle
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Status = "Active",
            NextAttemptAt = DateTime.UtcNow.AddDays(1) // Future
        });
        await ctx.SaveChangesAsync();

        await sut.ProcessDunningCyclesAsync();

        ctx.ChangeTracker.Clear();
        var cycle = ctx.DunningCycles.First();
        cycle.AttemptCount.Should().Be(0); // Unchanged
    }

    [Fact]
    public async Task ProcessDunningCyclesAsync_WhenCycleDue_IncrementsAttemptCount()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DunningService(ctx, _loggerMock.Object);
        var cycleId = Guid.NewGuid();

        ctx.DunningCycles.Add(new DunningCycle
        {
            Id = cycleId,
            TenantId = Guid.NewGuid(),
            Status = "Active",
            NextAttemptAt = DateTime.UtcNow.AddMinutes(-1), // Past — due
            AttemptCount = 0
        });
        await ctx.SaveChangesAsync();

        await sut.ProcessDunningCyclesAsync();

        ctx.ChangeTracker.Clear();
        var cycle = ctx.DunningCycles.First(d => d.Id == cycleId);
        cycle.AttemptCount.Should().Be(1);
        cycle.NextAttemptAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessDunningCyclesAsync_WhenAttemptCountReaches3_MarksAsFailed()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DunningService(ctx, _loggerMock.Object);
        var cycleId = Guid.NewGuid();

        ctx.DunningCycles.Add(new DunningCycle
        {
            Id = cycleId,
            TenantId = Guid.NewGuid(),
            Status = "Active",
            NextAttemptAt = DateTime.UtcNow.AddMinutes(-1),
            AttemptCount = 2 // Will become 3 = threshold
        });
        await ctx.SaveChangesAsync();

        await sut.ProcessDunningCyclesAsync();

        ctx.ChangeTracker.Clear();
        var cycle = ctx.DunningCycles.First(d => d.Id == cycleId);
        cycle.Status.Should().Be("Failed");
        cycle.AttemptCount.Should().Be(3);
    }

    [Fact]
    public async Task ProcessDunningCyclesAsync_IgnoresNonActiveCycles()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DunningService(ctx, _loggerMock.Object);

        ctx.DunningCycles.Add(new DunningCycle
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Status = "Completed",
            NextAttemptAt = DateTime.UtcNow.AddMinutes(-1),
            AttemptCount = 0
        });
        await ctx.SaveChangesAsync();

        await sut.ProcessDunningCyclesAsync();

        ctx.ChangeTracker.Clear();
        ctx.DunningCycles.First().AttemptCount.Should().Be(0); // Not touched
    }
}
