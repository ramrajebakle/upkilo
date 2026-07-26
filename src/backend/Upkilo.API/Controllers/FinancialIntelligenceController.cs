using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Filters;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "EnterpriseAdmin,Admin")]
    [ReadReplicaFilter] // SC1: financial queries routed to read replica
    public class FinancialIntelligenceController : ControllerBase
    {
        private readonly IFinancialProjectionService _projectionService;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public FinancialIntelligenceController(
            IFinancialProjectionService projectionService, 
            ITenantProvider tenantProvider,
            AppDbContext context)
        {
            _projectionService = projectionService;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId() 
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Get revenue prediction for the next X months
        /// </summary>
        [HttpGet("predict-revenue")]
        public async Task<IActionResult> PredictRevenue([FromQuery] int months = 3)
        {
            var projection = await _projectionService.PredictRevenueAsync(GetTenantId(), months);
            return Ok(new { projectedRevenue = projection, currency = "USD", monthsAhead = months });
        }

        /// <summary>
        /// Identify clients at risk of churning
        /// </summary>
        [HttpGet("churn-risk")]
        public async Task<IActionResult> GetChurnRisk()
        {
            var risks = await _projectionService.PredictChurnRiskAsync(GetTenantId());
            return Ok(risks);
        }

        /// <summary>
        /// Get 30-day cashflow forecast
        /// </summary>
        [HttpGet("cashflow-forecast")]
        public async Task<IActionResult> GetCashflowForecast()
        {
            var forecast = await _projectionService.GetCashflowForecastAsync(GetTenantId());
            return Ok(forecast);
        }

        /// <summary>
        /// Get tax liability report for a specific period
        /// </summary>
        [HttpGet("tax-report")]
        public async Task<IActionResult> GetTaxReport([FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
        {
            var startDate = start ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = end ?? DateTime.UtcNow;
            
            var report = await _projectionService.GenerateTaxReportAsync(GetTenantId(), startDate, endDate);
            return Ok(report);
        }

        /// <summary>
        /// Get revenue breakdown by service
        /// </summary>
        [HttpGet("revenue-by-service")]
        public async Task<IActionResult> GetRevenueByService([FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
        {
            var tenantId = GetTenantId();
            var startDate = start ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = end ?? DateTime.UtcNow;

            var stats = await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.TenantId == tenantId && b.StartTime >= startDate && b.StartTime <= endDate && b.Status == BookingStatus.Completed)
                .GroupBy(b => new { b.ServiceId, b.Service!.Name })
                .Select(g => new
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.Name,
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.Price ?? 0)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return Ok(new { startDate, endDate, data = stats });
        }
    }
}
