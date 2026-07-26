using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class PredictiveAnalyticsService
{
    private readonly ILogger<PredictiveAnalyticsService> _logger;
    private readonly AppDbContext _context;

    public PredictiveAnalyticsService(ILogger<PredictiveAnalyticsService> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<PredictionResult> PredictBookingNoShowAsync(Guid bookingId)
    {
        _logger.LogInformation("Running no-show prediction for booking {Id}", bookingId);

        var booking = await _context.Bookings
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return new PredictionResult { Probability = 0, Signal = "Unknown" };

        // Statistical model using historical no-show rate for this client
        double probability = 0.10; // population base rate

        if (booking.ClientId.HasValue)
        {
            var clientId = booking.ClientId.Value;
            var totalPast = await _context.Bookings
                .Where(b => b.ClientId == clientId && b.Id != bookingId && b.StartTime < DateTime.UtcNow)
                .CountAsync();

            if (totalPast > 0)
            {
                var noShows = await _context.Bookings
                    .Where(b => b.ClientId == clientId && b.Status == BookingStatus.NoShow)
                    .CountAsync();

                // Weighted blend: 70% client history, 30% base rate
                probability = (0.7 * ((double)noShows / totalPast)) + (0.3 * 0.10);
            }
        }

        // Boost probability for bookings with short lead time (< 2h) — higher no-show rate
        var leadTime = booking.StartTime - DateTime.UtcNow;
        if (leadTime.TotalHours < 2 && leadTime.TotalHours > 0)
            probability = Math.Min(1.0, probability * 1.4);

        // Boost for late evening slots (after 7pm)
        if (booking.StartTime.Hour >= 19)
            probability = Math.Min(1.0, probability * 1.15);

        probability = Math.Round(Math.Min(1.0, probability), 3);
        var signal = probability >= 0.5 ? "High Risk" : probability >= 0.25 ? "Medium Risk" : "Low Risk";

        return new PredictionResult { Probability = probability, Signal = signal };
    }

    public async Task<List<MetricForecast>> ForecastRevenueAsync(Guid tenantId, int monthsAhead)
    {
        _logger.LogInformation("Generating revenue forecast for tenant {TenantId}, {Months} months", tenantId, monthsAhead);

        // Fetch last 12 months of completed-booking revenue
        var historyStart = DateTime.UtcNow.AddMonths(-12);
        var monthly = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.Status == BookingStatus.Completed && b.StartTime >= historyStart)
            .GroupBy(b => new { b.StartTime.Year, b.StartTime.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(b => b.Price ?? 0m)
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        if (!monthly.Any())
            return new List<MetricForecast>();

        // Simple linear trend extrapolation (least-squares slope on last 12 months)
        var values = monthly.Select(m => (double)m.Revenue).ToList();
        var n = values.Count;
        var sumX = (double)n * (n - 1) / 2;
        var sumX2 = (double)(n - 1) * n * (2 * n - 1) / 6;
        var sumY = values.Sum();
        var sumXY = values.Select((v, i) => i * v).Sum();
        var slope = n > 1 ? (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX) : 0;
        var intercept = (sumY - slope * sumX) / n;

        var forecasts = new List<MetricForecast>();
        var baseDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        for (int i = 1; i <= monthsAhead; i++)
        {
            var forecastIndex = n + i - 1;
            var predicted = intercept + slope * forecastIndex;
            forecasts.Add(new MetricForecast
            {
                TargetMonth = baseDate.AddMonths(i),
                PredictedValue = Math.Max(0, (decimal)Math.Round(predicted, 2))
            });
        }

        return forecasts;
    }

    public async Task<ClientLtvResult> PredictClientLtvAsync(Guid tenantId, Guid clientId)
    {
        _logger.LogInformation("Predicting LTV for client {ClientId} in tenant {TenantId}", clientId, tenantId);

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.ClientId == clientId &&
                        b.Status == BookingStatus.Completed && !b.IsDeleted)
            .OrderBy(b => b.StartTime)
            .Select(b => new { b.StartTime, b.Price })
            .ToListAsync();

        var totalBookings = bookings.Count;
        var totalRevenue = bookings.Sum(b => b.Price ?? 0m);

        if (totalBookings == 0)
        {
            return new ClientLtvResult
            {
                EstimatedAnnualLtv = 0m,
                ChurnRiskScore = 0.8,
                TotalHistoricRevenue = 0m,
                AverageBookingValue = 0m,
                TotalBookings = 0,
                PredictionSignal = "Insufficient data"
            };
        }

        var avgBookingValue = totalRevenue / totalBookings;

        // Average interval between consecutive bookings (days)
        double avgIntervalDays = 30; // default assumption
        if (totalBookings >= 2)
        {
            var intervals = new List<double>();
            for (int i = 1; i < bookings.Count; i++)
                intervals.Add((bookings[i].StartTime - bookings[i - 1].StartTime).TotalDays);
            avgIntervalDays = intervals.Average();
        }

        // LTV formula: (averageRevenue * 12) / (avgIntervalDays / 30 + 1)
        var estimatedAnnualLtv = ((double)avgBookingValue * 12.0) / (avgIntervalDays / 30.0 + 1.0);

        // Churn risk based on last booking date
        var lastBooking = bookings.Last().StartTime;
        var daysSinceLast = (DateTime.UtcNow - lastBooking).TotalDays;
        var churnRisk = daysSinceLast > 90 ? 0.8 :
                        daysSinceLast > 60 ? 0.5 :
                        daysSinceLast > 30 ? 0.3 : 0.1;

        var signal = churnRisk >= 0.8 ? "High churn risk" :
                     churnRisk >= 0.5 ? "Moderate churn risk" :
                     churnRisk >= 0.3 ? "Low churn risk" : "Active client";

        return new ClientLtvResult
        {
            EstimatedAnnualLtv = (decimal)Math.Round(estimatedAnnualLtv, 2),
            ChurnRiskScore = churnRisk,
            TotalHistoricRevenue = totalRevenue,
            AverageBookingValue = avgBookingValue,
            TotalBookings = totalBookings,
            PredictionSignal = signal
        };
    }
}

public class ClientLtvResult
{
    public decimal EstimatedAnnualLtv { get; set; }
    public double ChurnRiskScore { get; set; }
    public decimal TotalHistoricRevenue { get; set; }
    public decimal AverageBookingValue { get; set; }
    public int TotalBookings { get; set; }
    public string PredictionSignal { get; set; } = string.Empty;
}

public class PredictionResult
{
    public double Probability { get; set; }
    public string Signal { get; set; } = "Unknown";
}

public class MetricForecast
{
    public DateTime TargetMonth { get; set; }
    public decimal PredictedValue { get; set; }
}
