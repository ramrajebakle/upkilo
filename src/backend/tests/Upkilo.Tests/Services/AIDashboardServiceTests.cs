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

public class AIDashboardServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public AIDashboardServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIDashboardService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDecisionLogsAsync_NoLogs_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIDashboardService(ctx);

        var result = await svc.GetDecisionLogsAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_NoData_ReturnsZeroStats()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIDashboardService(ctx);

        var result = await svc.GetDashboardMetricsAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.TotalDecisions.Should().Be(0);
        result.PendingReviews.Should().Be(0);
        result.TotalCost.Should().Be(0);
    }

    [Fact]
    public async Task LogDecisionAsync_ValidData_PersistsWithoutThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIDashboardService(ctx);

        var tenantId = Guid.NewGuid();
        var act = async () => await svc.LogDecisionAsync(
            tenantId, "test_agent", "Allow", "input text", "output text", 0.9m);
        await act.Should().NotThrowAsync();

        var logs = await svc.GetDecisionLogsAsync(tenantId);
        logs.Should().HaveCount(1);
    }

    public void Dispose() => _dbFactory.Dispose();
}
