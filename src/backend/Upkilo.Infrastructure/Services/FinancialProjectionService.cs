using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services
{
    public class FinancialProjectionService : IFinancialProjectionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FinancialProjectionService> _logger;

        public FinancialProjectionService(AppDbContext context, ILogger<FinancialProjectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> PredictRevenueAsync(Guid tenantId, int monthsAhead)
        {
            _logger.LogInformation("Predicting revenue for tenant {TenantId} for {Months} months ahead", tenantId, monthsAhead);
            
            // Fetch last 6 months of revenue
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var historicalRevenueList = await _context.Invoices
                .Where(i => i.TenantId == tenantId && i.IssuedAt >= sixMonthsAgo && i.Status == InvoiceStatus.Paid)
                .Select(i => i.TotalAmount)
                .ToListAsync();
            var historicalRevenue = historicalRevenueList.Sum();

            // Simple linear projection (Average * months)
            // In production, we would use a proper ML model here
            var monthlyAvg = historicalRevenue / 6;
            var projection = monthlyAvg * monthsAhead;

            return Math.Round(projection, 2);
        }

        public async Task<IEnumerable<ChurnRisk>> PredictChurnRiskAsync(Guid tenantId)
        {
            _logger.LogInformation("Analyzing churn risk for tenant {TenantId}", tenantId);
            
            // Logic: Identify clients who haven't booked in 60 days but used to book frequently
            var thresholdDate = DateTime.UtcNow.AddDays(-60);
            
            var atRiskClients = await _context.Clients
                .Where(c => c.TenantId == tenantId)
                .Select(c => new
                {
                    Client = c,
                    LastBooking = _context.Bookings
                        .Where(b => b.ClientId == c.Id)
                        .OrderByDescending(b => b.StartTime)
                        .FirstOrDefault()
                })
                .Where(x => x.LastBooking != null && x.LastBooking.StartTime < thresholdDate)
                .Take(10)
                .ToListAsync();

            return atRiskClients.Select(x => new ChurnRisk
            {
                ClientId = x.Client.Id,
                ClientName = $"{x.Client.FirstName} {x.Client.LastName}",
                RiskScore = 0.85, // High risk
                PrimaryReason = "No activity in last 60 days"
            });
        }

        public async Task<CashflowForecast> GetCashflowForecastAsync(Guid tenantId)
        {
            _logger.LogInformation("Generating cashflow forecast for tenant {TenantId}", tenantId);
            
            var forecast = new CashflowForecast
            {
                TenantId = tenantId,
                ForecastPoints = new List<ForecastPoint>()
            };

            // Pull daily revenue from the last 30 days to establish a baseline trend
            var historicalStart = DateTime.UtcNow.AddDays(-30).Date;
            var historicalInvoices = await _context.Invoices
                .Where(i => i.TenantId == tenantId && i.IssuedAt >= historicalStart && i.Status == InvoiceStatus.Paid)
                .Select(i => new { i.IssuedAt, i.TotalAmount })
                .ToListAsync();

            var dailyRevenue = historicalInvoices
                .GroupBy(i => i.IssuedAt.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(i => i.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToList();

            var avgDailyRevenue = dailyRevenue.Count > 0
                ? dailyRevenue.Average(d => d.Total)
                : 0m;

            // Simple linear trend: compute slope from historical data
            decimal slope = 0m;
            if (dailyRevenue.Count > 1)
            {
                var n = dailyRevenue.Count;
                var sumX = dailyRevenue.Select((_, i) => (decimal)i).Sum();
                var sumY = dailyRevenue.Sum(d => d.Total);
                var sumXY = dailyRevenue.Select((d, i) => i * d.Total).Sum();
                var sumX2 = dailyRevenue.Select((_, i) => (decimal)(i * i)).Sum();
                slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            }

            // Estimate daily expenses as 60% of average revenue (configurable baseline)
            var avgDailyExpenses = avgDailyRevenue * 0.60m;

            var start = DateTime.UtcNow;
            for (int i = 0; i < 30; i++)
            {
                var projectedRevenue = Math.Max(0, avgDailyRevenue + (slope * (i + 1)));
                forecast.ForecastPoints.Add(new ForecastPoint
                {
                    Date               = start.AddDays(i),
                    ProjectedRevenue   = Math.Round(projectedRevenue, 2),
                    ProjectedExpenses  = Math.Round(avgDailyExpenses, 2),
                    ConfidenceInterval = Math.Max(0.50, 0.95 - (i * 0.015))
                });
            }

            return forecast;
        }

        public async Task<TaxReport> GenerateTaxReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Generating tax report for tenant {TenantId} from {Start} to {End}", tenantId, startDate, endDate);

            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.TenantId == tenantId && b.StartTime >= startDate && b.StartTime <= endDate && b.Status == Upkilo.Core.Entities.BookingStatus.Completed)
                .ToListAsync();

            var defaultTaxRate = await _context.TaxRates
                .Where(t => t.TenantId == tenantId && t.IsDefault && t.IsActive)
                .Select(t => t.Percentage)
                .FirstOrDefaultAsync();

            if (defaultTaxRate == 0) defaultTaxRate = 8.0m; // Default 8% if not configured

            var taxReport = new TaxReport
            {
                TotalRevenue = bookings.Sum(b => b.Price ?? 0),
                TaxRate = defaultTaxRate,
                TaxLiability = bookings.Sum(b => (b.Price ?? 0) * (defaultTaxRate / 100))
            };

            taxReport.Breakdown = bookings
                .GroupBy(b => b.Service?.Category ?? "General")
                .Select(g => new TaxBreakdown
                {
                    Category = g.Key,
                    Amount = g.Sum(b => b.Price ?? 0),
                    TaxAmount = g.Sum(b => (b.Price ?? 0) * (defaultTaxRate / 100))
                })
                .ToList();

            return taxReport;
        }
    }
}
