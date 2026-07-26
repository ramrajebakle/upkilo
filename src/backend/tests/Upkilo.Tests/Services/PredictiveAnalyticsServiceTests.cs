using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class PredictiveAnalyticsServiceTests : IDisposable
{
    private readonly Mock<ILogger<PredictiveAnalyticsService>> _loggerMock = new();
    private readonly TestDbContextFactory _dbFactory = new();

    private PredictiveAnalyticsService CreateSut() =>
        new(_loggerMock.Object, _dbFactory.CreateContext());

    [Fact]
    public async Task PredictBookingNoShowAsync_UnknownBooking_ReturnsZeroProbability()
    {
        var service = CreateSut();
        var bookingId = Guid.NewGuid();

        var result = await service.PredictBookingNoShowAsync(bookingId);

        result.Should().NotBeNull();
        result.Probability.Should().Be(0);
        result.Signal.Should().Be("Unknown");
    }

    [Fact]
    public async Task ForecastRevenueAsync_NoHistory_ReturnsEmptyList()
    {
        var service = CreateSut();
        var tenantId = Guid.NewGuid();

        var result = await service.ForecastRevenueAsync(tenantId, 6);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PredictClientLtvAsync_NoBookings_ReturnsHighChurnRisk()
    {
        var service = CreateSut();

        var result = await service.PredictClientLtvAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().NotBeNull();
        result.EstimatedAnnualLtv.Should().Be(0m);
        result.ChurnRiskScore.Should().Be(0.8);
        result.TotalBookings.Should().Be(0);
    }

    public void Dispose() => _dbFactory.Dispose();
}
