using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Infrastructure.Services.Security;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class AutomationSafetyServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<AutomationSafetyService>> _loggerMock;

    public AutomationSafetyServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<AutomationSafetyService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AutomationSafetyService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateCampaignHealthAsync_UnknownTenant_ReturnsLowRiskResult()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AutomationSafetyService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var result = await svc.EvaluateCampaignHealthAsync(tenantId);

        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenantId);
        // No data means no traffic drops or error rates — should be Low risk
        result.OverallRisk.Should().Be(AutomationRiskLevel.Low);
        result.Actions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSafetyMetrics_NoData_ReturnsDefaults()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AutomationSafetyService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var result = await svc.EvaluateCampaignHealthAsync(tenantId, null);

        result.Should().NotBeNull();
        result.Actions.Should().BeEmpty();
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EnforceSafetyActionsAsync_NoActions_ReturnsZero()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AutomationSafetyService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var checkResult = new SafetyCheckResult { TenantId = tenantId };

        var count = await svc.EnforceSafetyActionsAsync(tenantId, checkResult);

        count.Should().Be(0);
    }

    public void Dispose() => _dbFactory.Dispose();
}
