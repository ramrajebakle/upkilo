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

public class CampaignAnalyticsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<CampaignAnalyticsService>> _loggerMock;

    public CampaignAnalyticsServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<CampaignAnalyticsService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new CampaignAnalyticsService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAnalyticsAsync_UnknownCampaign_ReturnsDefaultAnalytics()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new CampaignAnalyticsService(ctx, _loggerMock.Object);

        var campaignId = Guid.NewGuid();
        var result = await svc.GetAnalyticsAsync(campaignId);

        // Service returns a default analytics object when not found
        result.Should().NotBeNull();
        result!.CampaignId.Should().Be(campaignId);
    }

    [Fact]
    public async Task RecordEventAsync_ValidData_DoesNotThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new CampaignAnalyticsService(ctx, _loggerMock.Object);

        var campaignId = Guid.NewGuid();
        var act = async () => await svc.RecordEventAsync(campaignId, "click");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RecordEventAsync_OpenEvent_IncrementsOpenedCount()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "test-analytics" });
        ctx.Campaigns.Add(new Campaign
        {
            Id = campaignId,
            TenantId = tenantId,
            Name = "Summer Promo",
            Type = "email",
            Status = "Active"
        });
        ctx.SaveChanges();

        var svc = new CampaignAnalyticsService(ctx, _loggerMock.Object);
        await svc.RecordEventAsync(campaignId, "open");

        var analytics = await svc.GetAnalyticsAsync(campaignId);
        analytics.Should().NotBeNull();
        analytics!.OpenedCount.Should().Be(1);
    }

    public void Dispose() => _dbFactory.Dispose();
}
