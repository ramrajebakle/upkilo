using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;


namespace Upkilo.API.Controllers;

/// <summary>
/// Revenue & MRR tracking for the Super Admin dashboard.
/// Provides real-time revenue metrics, growth rates, and forecasting.
/// </summary>
[ApiController]
[Route("api/v1/admin/revenue")]
[Authorize(Roles = "SuperAdmin")]
[ApiVersion("1.0")]
public class RevenueTrackingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<RevenueTrackingController> _logger;

    public RevenueTrackingController(AppDbContext context, ILogger<RevenueTrackingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current MRR (Monthly Recurring Revenue) and growth metrics
    /// </summary>
    [HttpGet("mrr")]
    public async Task<IActionResult> GetMRR()
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = thisMonth.AddMonths(-1);

        var activeSubscriptions = await _context.Subscriptions
            .Include(ts => ts.PricingPlan).ThenInclude(p => p!.Prices)
            .Where(ts => ts.Status == SubscriptionStatus.Active || ts.Status == SubscriptionStatus.Trial)
            .ToListAsync();

        static decimal MonthlyPrice(Upkilo.Core.Entities.Subscription s)
            => s.PricingPlan?.Prices.FirstOrDefault(p => p.Cycle == Upkilo.Core.Entities.BillingCycle.Monthly)?.Amount ?? 0;

        var currentMRR = activeSubscriptions.Sum(MonthlyPrice);

        // Get last month's MRR for comparison
        var lastMonthSubs = await _context.Subscriptions
            .Include(ts => ts.PricingPlan).ThenInclude(p => p!.Prices)
            .Where(ts => ts.CreatedAt < lastMonth
                         && (ts.Status == SubscriptionStatus.Active || ts.Status == SubscriptionStatus.Trial))
            .ToListAsync();

        var previousMRR = lastMonthSubs.Sum(MonthlyPrice);
        var mrrGrowth = previousMRR > 0 ? Math.Round((currentMRR - previousMRR) / previousMRR * 100, 1) : 0;

        // Breakdown by plan
        var byPlan = activeSubscriptions
            .GroupBy(s => s.PricingPlan?.Name ?? "Unknown")
            .Select(g => new
            {
                plan = g.Key,
                subscribers = g.Count(),
                mrr = g.Sum(MonthlyPrice)
            })
            .OrderByDescending(x => x.mrr);

        // New subscriptions this month
        var newThisMonth = await _context.Subscriptions
            .Where(ts => ts.CreatedAt >= thisMonth)
            .CountAsync();

        // Churn this month
        var churnedThisMonth = await _context.Subscriptions
            .Where(ts => ts.CancelledAt.HasValue && ts.CancelledAt >= thisMonth)
            .CountAsync();

        return Ok(new
        {
            currentMRR,
            previousMRR,
            mrrGrowthPercent = mrrGrowth,
            arr = currentMRR * 12,  // Annual Recurring Revenue
            totalActiveSubscriptions = activeSubscriptions.Count,
            newSubscriptionsThisMonth = newThisMonth,
            churnedThisMonth,
            churnRate = activeSubscriptions.Count > 0
                ? Math.Round((double)churnedThisMonth / activeSubscriptions.Count * 100, 2)
                : 0,
            byPlan,
            projectedNextMonthMRR = currentMRR * (1 + (decimal)mrrGrowth / 100)
        });
    }

    /// <summary>
    /// Get revenue trend over the last N months
    /// </summary>
    [HttpGet("trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int months = 12)
    {
        months = Math.Clamp(months, 1, 24);
        var now = DateTime.UtcNow;
        var trend = new List<object>();

        for (int i = months - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            var activeSubs = await _context.Subscriptions
                .Include(ts => ts.PricingPlan).ThenInclude(p => p!.Prices)
                .Where(ts => ts.CreatedAt < monthEnd
                             && (ts.CancelledAt == null || ts.CancelledAt >= monthStart)
                             && (ts.Status == SubscriptionStatus.Active || ts.Status == SubscriptionStatus.Trial))
                .ToListAsync();

            var newSubs = await _context.Subscriptions
                .Where(ts => ts.CreatedAt >= monthStart && ts.CreatedAt < monthEnd)
                .CountAsync();

            var churned = await _context.Subscriptions
                .Where(ts => ts.CancelledAt >= monthStart && ts.CancelledAt < monthEnd)
                .CountAsync();

            trend.Add(new
            {
                month = monthStart.ToString("yyyy-MM"),
                mrr = activeSubs.Sum(s => s.PricingPlan?.Prices.FirstOrDefault(p => p.Cycle == Upkilo.Core.Entities.BillingCycle.Monthly)?.Amount ?? 0),
                activeSubscriptions = activeSubs.Count,
                newSubscriptions = newSubs,
                churned
            });
        }

        return Ok(new { months, trend });
    }

    /// <summary>
    /// Get user growth metrics
    /// </summary>
    [HttpGet("growth")]
    public async Task<IActionResult> GetUserGrowth()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive);
        var totalUsers = await _context.Users.CountAsync();

        var newTenantsLast30 = await _context.Tenants
            .CountAsync(t => t.CreatedAt >= thirtyDaysAgo);
        var newTenantsPrev30 = await _context.Tenants
            .CountAsync(t => t.CreatedAt >= sixtyDaysAgo && t.CreatedAt < thirtyDaysAgo);

        var tenantGrowthRate = newTenantsPrev30 > 0
            ? Math.Round((double)(newTenantsLast30 - newTenantsPrev30) / newTenantsPrev30 * 100, 1)
            : 0;

        return Ok(new
        {
            totalTenants,
            activeTenants,
            inactiveTenants = totalTenants - activeTenants,
            totalUsers,
            newTenantsLast30Days = newTenantsLast30,
            tenantGrowthRatePercent = tenantGrowthRate,
            averageUsersPerTenant = totalTenants > 0
                ? Math.Round((double)totalUsers / totalTenants, 1)
                : 0
        });
    }
}
