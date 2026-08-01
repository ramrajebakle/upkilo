using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ApiVersion("1.0")]
public class UsageDashboardController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UsageDashboardController> _logger;

    public UsageDashboardController(
        ISubscriptionService subscriptionService,
        ITenantProvider tenantProvider,
        AppDbContext dbContext,
        ILogger<UsageDashboardController> logger)
    {
        _subscriptionService = subscriptionService;
        _tenantProvider = tenantProvider;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive usage dashboard data
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UsageDashboardDto>> GetDashboard()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var usage = await _subscriptionService.GetUsageAsync(tenantId.Value);
        var subscription = await _subscriptionService.GetSubscriptionAsync(tenantId.Value);
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId.Value);

        // Get AI usage breakdown
        var aiBreakdown = await GetAiUsageBreakdownAsync(tenantId.Value, usage.PeriodStart, usage.PeriodEnd);

        // Get usage trend (last 30 days)
        var trend = await GetUsageTrendAsync(tenantId.Value);

        return Ok(new UsageDashboardDto
        {
            Summary = usage,
            Subscription = subscription != null ? new SubscriptionInfoDto
            {
                PlanName = subscription.PricingPlan?.Name ?? "Free",
                Status = subscription.Status.ToString(),
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                IsTrialing = subscription.Status == SubscriptionStatus.Trialing,
                TrialEndsAt = tenant?.TrialEndsAt
            } : null,
            AiBreakdown = aiBreakdown,
            UsageTrend = trend,
            Alerts = GenerateAlerts(usage)
        });
    }

    /// <summary>
    /// Get usage history over time
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<UsageHistoryPoint>>> GetHistory(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string granularity = "day") // day, week, month
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var history = await GetUsageHistoryAsync(tenantId.Value, startDate, endDate, granularity);
        return Ok(history);
    }

    /// <summary>
    /// Get AI usage breakdown by feature
    /// </summary>
    [HttpGet("ai")]
    public async Task<ActionResult<AiUsageBreakdown>> GetAiUsage(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var breakdown = await GetAiUsageBreakdownAsync(tenantId.Value, startDate, endDate);
        return Ok(breakdown);
    }

    /// <summary>
    /// Export usage report as CSV
    /// </summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportUsage(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return BadRequest("Tenant not found");

        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var history = await GetUsageHistoryAsync(tenantId.Value, startDate, endDate, "day");

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Date,Bookings,SMS,AICredits,StorageGB");

        foreach (var point in history)
        {
            csv.AppendLine($"{point.Date:yyyy-MM-dd},{point.Bookings},{point.Sms},{point.AiCredits},{point.StorageGb:F2}");
        }

        var fileName = $"usage-report-{tenantId.Value:N}-{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private async Task<AiUsageBreakdown> GetAiUsageBreakdownAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var logs = await _dbContext.Set<AIUsageLog>()
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= from && l.CreatedAt <= to)
            .ToListAsync();

        return new AiUsageBreakdown
        {
            TotalCredits = logs.Sum(l => l.InputTokens + l.OutputTokens),
            TotalCost = logs.Sum(l => l.Cost),
            ByFeature = logs
                .GroupBy(l => l.Feature)
                .ToDictionary(g => g.Key, g => new FeatureUsage
                {
                    Credits = g.Sum(l => l.InputTokens + l.OutputTokens),
                    Cost = g.Sum(l => l.Cost),
                    Requests = g.Count()
                }),
            ByModel = logs
                .GroupBy(l => l.Model)
                .ToDictionary(g => g.Key, g => new ModelUsage
                {
                    InputTokens = g.Sum(l => l.InputTokens),
                    OutputTokens = g.Sum(l => l.OutputTokens),
                    Cost = g.Sum(l => l.Cost),
                    AvgLatencyMs = (int)(g.Where(l => l.LatencyMs.HasValue).Average(l => l.LatencyMs) ?? 0)
                }),
            SuccessRate = logs.Count > 0
                ? (decimal)logs.Count(l => l.Success) / logs.Count * 100
                : 100
        };
    }

    private async Task<List<UsageTrendPoint>> GetUsageTrendAsync(Guid tenantId)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-6);
        var endDate = DateTime.UtcNow.Date.AddDays(1);

        var bookingCounts = await _dbContext.Bookings
            .Where(b => b.TenantId == tenantId && b.StartTime >= startDate && b.StartTime < endDate && !b.IsDeleted)
            .GroupBy(b => b.StartTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var aiCredits = await _dbContext.Set<AIUsageLog>()
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= startDate && l.CreatedAt < endDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Credits = g.Sum(l => l.InputTokens + l.OutputTokens) })
            .ToListAsync();

        var trend = new List<UsageTrendPoint>();
        for (int i = 0; i < 7; i++)
        {
            var date = startDate.AddDays(i);
            trend.Add(new UsageTrendPoint
            {
                Date = date,
                Bookings = bookingCounts.FirstOrDefault(x => x.Date == date)?.Count ?? 0,
                AiCredits = aiCredits.FirstOrDefault(x => x.Date == date)?.Credits ?? 0
            });
        }

        return trend;
    }

    private async Task<int> GetDailyBookingCountAsync(Guid tenantId, DateTime date)
    {
        var nextDate = date.AddDays(1);
        return await _dbContext.Bookings
            .IgnoreQueryFilters() // We'll handle TenantId explicitly if filter is too restrictive or just let it work
            .Where(b => b.TenantId == tenantId && b.StartTime >= date && b.StartTime < nextDate && !b.IsDeleted)
            .CountAsync();
    }

    private async Task<int> GetDailyAiCreditsAsync(Guid tenantId, DateTime date)
    {
        var nextDate = date.AddDays(1);
        return await _dbContext.Set<AIUsageLog>()
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= date && l.CreatedAt < nextDate)
            .SumAsync(l => l.InputTokens + l.OutputTokens);
    }

    private async Task<List<UsageHistoryPoint>> GetUsageHistoryAsync(
        Guid tenantId, DateTime from, DateTime to, string granularity)
    {
        var usage = await _subscriptionService.GetUsageAsync(tenantId);
        var history = new List<UsageHistoryPoint>();
        var current = from.Date;

        while (current <= to.Date)
        {
            var nextDate = granularity switch
            {
                "week" => current.AddDays(7),
                "month" => current.AddMonths(1),
                _ => current.AddDays(1)
            };

            var aiLogsSum = await _dbContext.AIUsageLogs
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == tenantId && l.CreatedAt >= current && l.CreatedAt < nextDate)
                .SumAsync(l => (int?)(l.InputTokens + l.OutputTokens)) ?? 0;

            var bookingCount = await _dbContext.Bookings
                .IgnoreQueryFilters()
                .Where(b => b.TenantId == tenantId && b.StartTime >= current && b.StartTime < nextDate && !b.IsDeleted)
                .CountAsync();

            // SMS tally from Notification table
            var smsCount = await _dbContext.Set<Notification>()
                .IgnoreQueryFilters()
                .Where(n => n.TenantId == tenantId && n.Type == NotificationType.Sms && n.CreatedAt >= current && n.CreatedAt < nextDate)
                .CountAsync();

            history.Add(new UsageHistoryPoint
            {
                Date = current,
                Bookings = bookingCount,
                Sms = smsCount,
                AiCredits = aiLogsSum,
                StorageGb = Math.Round(usage.StorageUsedBytes / (1024.0m * 1024.0m * 1024.0m), 2)
            });

            current = nextDate;
        }

        return history;
    }

    private List<UsageAlert> GenerateAlerts(UsageSummary usage)
    {
        var alerts = new List<UsageAlert>();

        // Check booking usage
        var bookingPercent = usage.BookingsLimit > 0
            ? (decimal)usage.BookingsUsed / usage.BookingsLimit * 100
            : 0;

        if (bookingPercent >= 100)
            alerts.Add(new UsageAlert { Type = "danger", Resource = "Bookings", Message = "Booking limit reached", Percentage = 100 });
        else if (bookingPercent >= 90)
            alerts.Add(new UsageAlert { Type = "warning", Resource = "Bookings", Message = "90% of booking quota used", Percentage = (int)bookingPercent });
        else if (bookingPercent >= 80)
            alerts.Add(new UsageAlert { Type = "info", Resource = "Bookings", Message = "80% of booking quota used", Percentage = (int)bookingPercent });

        // Check AI credits
        var aiPercent = usage.AiCreditsLimit > 0
            ? (decimal)usage.AiCreditsUsed / usage.AiCreditsLimit * 100
            : 0;

        if (aiPercent >= 100)
            alerts.Add(new UsageAlert { Type = "danger", Resource = "AI Credits", Message = "AI credit limit reached", Percentage = 100 });
        else if (aiPercent >= 90)
            alerts.Add(new UsageAlert { Type = "warning", Resource = "AI Credits", Message = "90% of AI credits used", Percentage = (int)aiPercent });

        // Check SMS
        var smsPercent = usage.SmsLimit > 0
            ? (decimal)usage.SmsUsed / usage.SmsLimit * 100
            : 0;

        if (smsPercent >= 90)
            alerts.Add(new UsageAlert { Type = "warning", Resource = "SMS", Message = "90% of SMS quota used", Percentage = (int)smsPercent });

        // Check AI budget
        if (usage.AiCostLimit > 0)
        {
            var aiCostPercent = usage.AiCostUsed / usage.AiCostLimit * 100;
            if (aiCostPercent >= 100)
                alerts.Add(new UsageAlert { Type = "danger", Resource = "AI Budget", Message = "AI monthly budget reached", Percentage = 100 });
            else if (aiCostPercent >= 90)
                alerts.Add(new UsageAlert { Type = "warning", Resource = "AI Budget", Message = "90% of AI budget used", Percentage = (int)aiCostPercent });
            else if (aiCostPercent >= 80)
                alerts.Add(new UsageAlert { Type = "info", Resource = "AI Budget", Message = "80% of AI budget used", Percentage = (int)aiCostPercent });
        }

        return alerts;
    }
}

#region DTOs

public class UsageDashboardDto
{
    public UsageSummary Summary { get; set; } = new();
    public SubscriptionInfoDto? Subscription { get; set; }
    public AiUsageBreakdown AiBreakdown { get; set; } = new();
    public List<UsageTrendPoint> UsageTrend { get; set; } = new();
    public List<UsageAlert> Alerts { get; set; } = new();
}

public class SubscriptionInfoDto
{
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public bool IsTrialing { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}

public class AiUsageBreakdown
{
    public int TotalCredits { get; set; }
    public decimal TotalCost { get; set; }
    public Dictionary<string, FeatureUsage> ByFeature { get; set; } = new();
    public Dictionary<string, ModelUsage> ByModel { get; set; } = new();
    public decimal SuccessRate { get; set; }
}

public class FeatureUsage
{
    public int Credits { get; set; }
    public decimal Cost { get; set; }
    public int Requests { get; set; }
}

public class ModelUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
    public int AvgLatencyMs { get; set; }
}

public class UsageTrendPoint
{
    public DateTime Date { get; set; }
    public int Bookings { get; set; }
    public int AiCredits { get; set; }
}

public class UsageHistoryPoint
{
    public DateTime Date { get; set; }
    public int Bookings { get; set; }
    public int Sms { get; set; }
    public int AiCredits { get; set; }
    public decimal StorageGb { get; set; }
}

public class UsageAlert
{
    public string Type { get; set; } = "info"; // info, warning, danger
    public string Resource { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Percentage { get; set; }
}

#endregion
