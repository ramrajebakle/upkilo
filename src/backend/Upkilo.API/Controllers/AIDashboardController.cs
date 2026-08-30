using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Attributes;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Entities;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("ai_insights")]
public class AIDashboardController : ControllerBase
{
    private readonly IAIDashboardService _dashboardService;
    private readonly ITenantProvider _tenantProvider;
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<AIDashboardController> _logger;

    public AIDashboardController(
        IAIDashboardService dashboardService,
        ITenantProvider tenantProvider,
        AppDbContext context,
        IAIService aiService,
        IPushNotificationService pushService,
        ILogger<AIDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _tenantProvider = tenantProvider;
        _context = context;
        _aiService = aiService;
        _pushService = pushService;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");
    private Guid GetUserId() => _tenantProvider.GetUserId()
        ?? throw new UnauthorizedAccessException("User context not available");

    /// <summary>
    /// Get AI Dashboard metrics for the tenant
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _dashboardService.GetDashboardMetricsAsync(GetTenantId());
        return Ok(metrics);
    }

    /// <summary>
    /// Get recent AI decision logs
    /// </summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int count = 20)
    {
        var logs = await _dashboardService.GetDecisionLogsAsync(GetTenantId(), count);
        return Ok(logs);
    }

    /// <summary>
    /// Approve an AI decision (human-in-the-loop)
    /// </summary>
    [HttpPost("approve/{id}")]
    public async Task<IActionResult> ApproveDecision(Guid id)
    {
        var success = await _dashboardService.ApproveDecisionAsync(GetTenantId(), id, GetUserId());
        if (!success) return NotFound();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Day 74: GET /api/v1/aidashboard/forecast — AI revenue forecast (30/60/90 day projections + recommendations).
    /// </summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetRevenueForecast(
        [FromServices] Upkilo.Infrastructure.Services.RevenueForecastService forecastService)
    {
        var forecast = await forecastService.GenerateForecastAsync(GetTenantId());
        return Ok(forecast);
    }

    /// <summary>
    /// Day 75: GET /api/v1/aidashboard/recommendations — Top 3 AI actions owner should take this week.
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(
        [FromServices] Upkilo.Infrastructure.Services.RevenueForecastService forecastService)
    {
        var forecast = await forecastService.GenerateForecastAsync(GetTenantId());
        return Ok(new { recommendations = forecast.AiRecommendations, generatedAt = forecast.GeneratedAt });
    }

    /// <summary>
    /// GET /api/v1/aidashboard/value-proof — ROI widget showing hours saved and cost equivalent.
    /// Displayed on the AI Dashboard to make AI value tangible for owners.
    /// Assumes 7 min saved per AI action at $50/hr freelancer equivalent.
    /// </summary>
    [HttpGet("value-proof")]
    public async Task<IActionResult> GetValueProof()
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalActions = await _context.AIUsageLogs
            .CountAsync(a => a.TenantId == tenantId);

        var actionsThisMonth = await _context.AIUsageLogs
            .CountAsync(a => a.TenantId == tenantId && a.CreatedAt >= monthStart);

        // 7 minutes saved per AI action (conservative: drafting, summarizing, analyzing)
        const double MinutesPerAction = 7.0;
        // $50/hr is the freelancer rate for admin / copywriting work
        const double HourlyRate = 50.0;

        var totalMinutesSaved = totalActions * MinutesPerAction;
        var totalHoursSaved = Math.Round(totalMinutesSaved / 60.0, 1);
        var totalCostEquivalent = Math.Round(totalHoursSaved * HourlyRate, 2);

        var monthMinutesSaved = actionsThisMonth * MinutesPerAction;
        var monthHoursSaved = Math.Round(monthMinutesSaved / 60.0, 1);
        var monthCostEquivalent = Math.Round(monthHoursSaved * HourlyRate, 2);

        return Ok(new
        {
            allTime = new
            {
                actionsCount = totalActions,
                hoursSaved = totalHoursSaved,
                costEquivalentUsd = totalCostEquivalent,
                headline = $"Saved {totalHoursSaved:F1} hours total — equivalent to ${totalCostEquivalent:F0} of freelance work"
            },
            thisMonth = new
            {
                actionsCount = actionsThisMonth,
                hoursSaved = monthHoursSaved,
                costEquivalentUsd = monthCostEquivalent,
                headline = $"This month: {actionsThisMonth} AI actions saved {monthHoursSaved:F1} hrs"
            },
            assumptions = new
            {
                minutesPerAction = MinutesPerAction,
                hourlyRateUsd = HourlyRate,
                methodology = "7 min/action based on admin tasks (drafting, summarizing, scheduling). $50/hr freelancer equivalent."
            },
            generatedAt = now
        });
    }

    /// <summary>
    /// A6: GET /api/v1/aidashboard/weekly-summary
    /// Returns an AI-narrated weekly business summary combining financial + booking + client data.
    /// Wires FinancialIntelligenceController metrics into a single human-readable narrative.
    /// </summary>
    [HttpGet("weekly-summary")]
    [RequiresFeature(FeatureKeys.AiInsights)]
    public async Task<IActionResult> GetWeeklySummary()
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var weekStart = now.AddDays(-7);
        var prevWeekStart = now.AddDays(-14);

        // Gather metrics in parallel
        var tRevenue = _context.Payments
            .Where(p => p.TenantId == tenantId && p.CreatedAt >= weekStart && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount);
        var tPrevRevenue = _context.Payments
            .Where(p => p.TenantId == tenantId && p.CreatedAt >= prevWeekStart && p.CreatedAt < weekStart && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount);
        var tNewBookings = _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.CreatedAt >= weekStart);
        var tPrevBookings = _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.CreatedAt >= prevWeekStart && b.CreatedAt < weekStart);
        var tNewClients = _context.Clients.CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= weekStart);
        var tAiActions = _context.AIUsageLogs.CountAsync(a => a.TenantId == tenantId && a.CreatedAt >= weekStart);

        await Task.WhenAll(tRevenue, tPrevRevenue, tNewBookings, tPrevBookings, tNewClients, tAiActions);

        var revenue = tRevenue.Result;
        var prevRevenue = tPrevRevenue.Result;
        var newBookings = tNewBookings.Result;
        var prevBookings = tPrevBookings.Result;
        var newClients = tNewClients.Result;
        var aiActions = tAiActions.Result;

        var revenueVal = revenue ?? 0m;
        var prevRevenueVal = prevRevenue ?? 0m;
        var revenueDelta = prevRevenueVal > 0 ? Math.Round((revenueVal - prevRevenueVal) / prevRevenueVal * 100, 1) : 0m;
        var bookingDelta = prevBookings > 0 ? Math.Round((decimal)(newBookings - prevBookings) / prevBookings * 100, 1) : 0m;

        var prompt =
            $"Write a concise, friendly 3-sentence weekly business summary for a service business owner.\n" +
            $"This week's metrics:\n" +
            $"- Revenue: ${revenueVal:F0} ({(revenueDelta >= 0 ? "+" : "")}{revenueDelta}% vs last week)\n" +
            $"- New bookings: {newBookings} ({(bookingDelta >= 0 ? "+" : "")}{bookingDelta}% vs last week)\n" +
            $"- New clients acquired: {newClients}\n" +
            $"- AI actions used: {aiActions}\n\n" +
            "Be encouraging, data-driven, and end with one specific actionable suggestion. Under 100 words.";

        var aiResult = await _aiService.GenerateTextAsync(tenantId, Guid.Empty, prompt);
        var narrative = aiResult.Success ? aiResult.Content?.Trim() ?? "" : "Your weekly summary is ready. Check the numbers below!";

        return Ok(new
        {
            weekOf = weekStart.ToString("yyyy-MM-dd"),
            narrative,
            metrics = new
            {
                revenueThisWeek = revenueVal,
                revenueChange = $"{(revenueDelta >= 0 ? "+" : "")}{revenueDelta}%",
                newBookings,
                bookingChange = $"{(bookingDelta >= 0 ? "+" : "")}{bookingDelta}%",
                newClients,
                aiActionsUsed = aiActions
            },
            generatedAt = now
        });
    }

    /// <summary>
    /// A7: POST /api/v1/aidashboard/weekly-summary/push
    /// Sends the weekly AI summary as a push notification to the current user.
    /// Typically called by a scheduled job (WeeklySummaryJob), but also callable manually.
    /// </summary>
    [HttpPost("weekly-summary/push")]
    [RequiresFeature(FeatureKeys.AiInsights)]
    public async Task<IActionResult> SendWeeklySummaryPush()
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var summaryResult = await GetWeeklySummary() as OkObjectResult;
        if (summaryResult?.Value is not object summaryObj)
            return StatusCode(500, new { error = "Could not generate summary." });

        // Serialize and pull out the narrative
        var json = System.Text.Json.JsonSerializer.Serialize(summaryObj);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var narrative = doc.RootElement.TryGetProperty("narrative", out var n) ? n.GetString() ?? "" : "Your weekly summary is ready.";

        await _pushService.SendPushToUserAsync(
            userId,
            "Your Weekly Business Summary",
            narrative.Length > 100 ? narrative[..97] + "..." : narrative);

        _logger.LogInformation("[A7] Weekly summary push sent to user {UserId} in tenant {TenantId}", userId, tenantId);

        return Ok(new { sent = true, userId, message = "Push notification sent." });
    }

}

