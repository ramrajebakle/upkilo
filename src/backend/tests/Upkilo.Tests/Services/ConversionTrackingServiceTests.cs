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

public class ConversionTrackingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public ConversionTrackingServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ConversionTrackingService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task TrackEventAsync_ValidEvent_PersistsToDb()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ConversionTrackingService(ctx);

        var tenantId = Guid.NewGuid();
        var evt = new ConversionEvent
        {
            EventName = "booking_confirmed",
            EventCategory = "booking",
            Source = "Organic",
            Platform = "web",
            Revenue = 50m
        };

        var act = async () => await svc.TrackEventAsync(tenantId, evt);
        await act.Should().NotThrowAsync();

        var from = DateTime.UtcNow.AddMinutes(-1);
        var to = DateTime.UtcNow.AddMinutes(1);
        var events = await svc.GetEventsAsync(tenantId, from, to);
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEventsAsync_NoEvents_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ConversionTrackingService(ctx);

        var result = await svc.GetEventsAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummaryAsync_NoEvents_ReturnsZeroTotals()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ConversionTrackingService(ctx);

        var result = await svc.GetSummaryAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        result.Should().NotBeNull();
        result.TotalEvents.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
    }

    public void Dispose() => _dbFactory.Dispose();
}
