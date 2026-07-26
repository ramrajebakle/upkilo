using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Computes the weekly Business Health Score and generates an AI-written narrative summary.
/// Score is 0-100 based on: revenue trend, new clients, retention rate, booking fill rate.
/// </summary>
public class BusinessHealthService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<BusinessHealthService> _logger;

    public BusinessHealthService(AppDbContext context, IAIService aiService, ILogger<BusinessHealthService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<BusinessHealthReport> GenerateReportAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var thisWeekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);

        // Revenue this week vs last week
        var thisWeekRevenue = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.Status == BookingStatus.Completed &&
                        b.StartTime >= thisWeekStart &&
                        b.PaymentStatus == PaymentStatus.Succeeded)
            .SumAsync(b => (decimal?)b.Price ?? 0);

        var lastWeekRevenue = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.Status == BookingStatus.Completed &&
                        b.StartTime >= lastWeekStart &&
                        b.StartTime < thisWeekStart &&
                        b.PaymentStatus == PaymentStatus.Succeeded)
            .SumAsync(b => (decimal?)b.Price ?? 0);

        // New clients this month vs last month
        var newClientsThisMonth = await _context.Clients
            .CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= monthStart);

        var newClientsLastMonth = await _context.Clients
            .CountAsync(c => c.TenantId == tenantId && c.CreatedAt >= lastMonthStart && c.CreatedAt < monthStart);

        // Retention rate: clients who have 2+ completed bookings
        var retainedClients = await _context.Clients
            .Where(c => c.TenantId == tenantId)
            .CountAsync(c => _context.Bookings
                .Count(b => b.ClientId == c.Id &&
                            b.Status == BookingStatus.Completed &&
                            b.StartTime >= now.AddDays(-60)) >= 1 &&
                _context.Bookings.Count(b => b.ClientId == c.Id && b.Status == BookingStatus.Completed) >= 2);

        var totalClients = await _context.Clients.CountAsync(c => c.TenantId == tenantId);
        var retentionRate = totalClients > 0 ? (double)retainedClients / totalClients * 100 : 0;

        // Booking fill rate for next 7 days
        var upcomingBookings = await _context.Bookings
            .CountAsync(b => b.TenantId == tenantId &&
                             b.StartTime >= now &&
                             b.StartTime <= now.AddDays(7) &&
                             b.Status != BookingStatus.Cancelled);

        var staffCount = Math.Max(1, await _context.Staff.CountAsync(s => s.TenantId == tenantId && s.IsActive));
        var theoreticalSlots = staffCount * 7 * 8; // 8 slots per day
        var fillRate = Math.Min(100, (double)upcomingBookings / theoreticalSlots * 100);

        // Compute health score (weighted)
        var revenueScore = lastWeekRevenue > 0
            ? Math.Min(50, (double)(thisWeekRevenue / lastWeekRevenue) * 50)
            : 25;
        var clientGrowthScore = newClientsLastMonth > 0
            ? Math.Min(25, (double)newClientsThisMonth / newClientsLastMonth * 25)
            : 12.5;
        var retentionScore = retentionRate * 0.15;
        var fillScore = fillRate * 0.10;

        var totalScore = (int)Math.Round(revenueScore + clientGrowthScore + retentionScore + fillScore);
        totalScore = Math.Clamp(totalScore, 0, 100);

        var grade = totalScore switch
        {
            >= 80 => "A",
            >= 65 => "B",
            >= 50 => "C",
            >= 35 => "D",
            _ => "F"
        };

        var revenueTrend = lastWeekRevenue > 0
            ? (double)((thisWeekRevenue - lastWeekRevenue) / lastWeekRevenue * 100)
            : 0;

        // Generate AI narrative
        string narrative;
        try
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            var aiPrompt = $"""
                Write a concise (3-4 sentences) weekly business health summary for {tenant?.Name ?? "this business"}.
                Data:
                - This week revenue: ${thisWeekRevenue:F0} ({revenueTrend:+0.0;-0.0}% vs last week)
                - New clients this month: {newClientsThisMonth} ({(newClientsThisMonth >= newClientsLastMonth ? "+" : "")}{newClientsThisMonth - newClientsLastMonth} vs last month)
                - Client retention rate: {retentionRate:F0}%
                - Next 7-day calendar fill: {fillRate:F0}%
                - Health score: {totalScore}/100 (Grade: {grade})

                Be encouraging but honest. Give one specific action they should take this week.
                Write in second person ("Your business...").
                """;

            var result = await _aiService.GenerateTextAsync(tenantId, Guid.Empty, aiPrompt);
            narrative = result.Content?.Trim() ?? BuildFallbackNarrative(totalScore, revenueTrend, newClientsThisMonth, retentionRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BusinessHealth] AI narrative failed for tenant {TenantId} — using fallback", tenantId);
            narrative = BuildFallbackNarrative(totalScore, revenueTrend, newClientsThisMonth, retentionRate);
        }

        return new BusinessHealthReport
        {
            TenantId = tenantId,
            Score = totalScore,
            Grade = grade,
            GeneratedAt = now,
            ThisWeekRevenue = thisWeekRevenue,
            LastWeekRevenue = lastWeekRevenue,
            RevenueTrendPercent = revenueTrend,
            NewClientsThisMonth = newClientsThisMonth,
            RetentionRatePercent = retentionRate,
            CalendarFillRatePercent = fillRate,
            AiNarrative = narrative,
            TopAction = GetTopAction(totalScore, revenueTrend, fillRate, retentionRate)
        };
    }

    private static string BuildFallbackNarrative(int score, double revenueTrend, int newClients, double retention)
    {
        var trend = revenueTrend >= 0 ? $"up {revenueTrend:F0}%" : $"down {Math.Abs(revenueTrend):F0}%";
        return $"Your business scored {score}/100 this week. Revenue is {trend} from last week " +
               $"and you added {newClients} new client{(newClients == 1 ? "" : "s")} this month. " +
               $"Client retention stands at {retention:F0}%. " +
               (score >= 70 ? "Great momentum — keep engaging your loyal clients." : "Focus on re-engaging lapsed clients this week.");
    }

    private static string GetTopAction(int score, double revenueTrend, double fillRate, double retentionRate) =>
        (score, fillRate, retentionRate) switch
        {
            _ when fillRate < 50 => "Run the Fill My Calendar AI to fill open slots with lapsed clients.",
            _ when retentionRate < 40 => "Send re-engagement messages to clients who haven't booked in 45+ days.",
            _ when revenueTrend < -10 => "Launch a flash promotion to boost this week's bookings.",
            _ when score >= 80 => "Ask your top 10 clients for Google reviews — you're doing great!",
            _ => "Set up automated follow-up messages for completed appointments."
        };
}

public class BusinessHealthReport
{
    public Guid TenantId { get; set; }
    public int Score { get; set; }
    public string Grade { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public decimal ThisWeekRevenue { get; set; }
    public decimal LastWeekRevenue { get; set; }
    public double RevenueTrendPercent { get; set; }
    public int NewClientsThisMonth { get; set; }
    public double RetentionRatePercent { get; set; }
    public double CalendarFillRatePercent { get; set; }
    public string AiNarrative { get; set; } = string.Empty;
    public string TopAction { get; set; } = string.Empty;
}
