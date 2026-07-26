using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Days 74-75: AI Business Intelligence — revenue forecasting + top 3 AI recommendations.
/// Forecast is based on trailing 60-day booking velocity × fill rate trend.
/// </summary>
public class RevenueForecastService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<RevenueForecastService> _logger;

    public RevenueForecastService(AppDbContext context, IAIService aiService, ILogger<RevenueForecastService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<RevenueForecast> GenerateForecastAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;

        // Trailing 60 days for baseline
        var last60Revenue = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.Status == BookingStatus.Completed &&
                        b.PaymentStatus == PaymentStatus.Succeeded &&
                        b.StartTime >= now.AddDays(-60))
            .SumAsync(b => (decimal?)b.Price ?? 0);

        var last30Revenue = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.Status == BookingStatus.Completed &&
                        b.PaymentStatus == PaymentStatus.Succeeded &&
                        b.StartTime >= now.AddDays(-30))
            .SumAsync(b => (decimal?)b.Price ?? 0);

        var prev30Revenue = last60Revenue - last30Revenue;

        var momGrowth = prev30Revenue > 0
            ? (double)((last30Revenue - prev30Revenue) / prev30Revenue * 100)
            : 0;

        // Confirmed future bookings value
        var confirmedFuture = await _context.Bookings
            .Where(b => b.TenantId == tenantId &&
                        b.Status == BookingStatus.Confirmed &&
                        b.StartTime >= now &&
                        b.StartTime <= now.AddDays(30))
            .SumAsync(b => (decimal?)b.Price ?? 0);

        // Membership recurring revenue
        var activeMembers = await _context.ClientMemberships
            .CountAsync(m => m.TenantId == tenantId &&
                             m.Status == MembershipStatus.Active &&
                             !m.IsDeleted);

        var avgMemberValue = activeMembers > 0
            ? await _context.ClientMemberships
                .Include(m => m.MembershipPlan)
                .Where(m => m.TenantId == tenantId && m.Status == MembershipStatus.Active)
                .AverageAsync(m => (decimal?)m.MembershipPlan.Price ?? 0)
            : 0;

        var membershipRevenue = activeMembers * avgMemberValue;

        // Forecast: confirmed + projected from velocity + membership
        var dailyVelocity = last30Revenue / 30;
        var forecast30 = confirmedFuture + (dailyVelocity * 15) + membershipRevenue; // half from velocity (conservative)
        var forecast60 = forecast30 * (1 + (decimal)(momGrowth / 100 * 0.5));
        var forecast90 = forecast30 * (1 + (decimal)(momGrowth / 100));

        // AI recommendations
        var aiRecommendations = await GetAiRecommendationsAsync(tenantId, last30Revenue, momGrowth, activeMembers);

        return new RevenueForecast
        {
            TenantId = tenantId,
            GeneratedAt = now,
            Last30DayRevenue = last30Revenue,
            MomGrowthPercent = momGrowth,
            ConfirmedNextMonthRevenue = confirmedFuture,
            MembershipMonthlyRevenue = membershipRevenue,
            Forecast30Days = Math.Max(0, forecast30),
            Forecast60Days = Math.Max(0, forecast60),
            Forecast90Days = Math.Max(0, forecast90),
            ActiveMemberCount = activeMembers,
            AiRecommendations = aiRecommendations
        };
    }

    private async Task<List<string>> GetAiRecommendationsAsync(Guid tenantId, decimal monthRevenue, double momGrowth, int members)
    {
        try
        {
            var prompt = $"""
                You are a business advisor for a service business. Give exactly 3 concise, actionable recommendations.
                Data:
                - Last 30 days revenue: ${monthRevenue:F0}
                - Month-over-month growth: {momGrowth:+0.0;-0.0}%
                - Active membership subscribers: {members}

                Rules:
                - Each recommendation must be 1 sentence, start with an action verb, be specific.
                - Focus on revenue growth, retention, and operations.
                - Return ONLY a JSON array of 3 strings, no other text.
                Example: ["Send a re-engagement SMS to clients inactive for 30+ days.", "Introduce a 3-session package deal at a 10% discount.", "Add an online booking widget to your Instagram bio."]
                """;

            var result = await _aiService.GenerateTextAsync(tenantId, Guid.Empty, prompt);
            var content = result.Content?.Trim() ?? "[]";

            var start = content.IndexOf('[');
            var end = content.LastIndexOf(']');
            if (start >= 0 && end > start)
            {
                var jsonArray = content[start..(end + 1)];
                var recs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonArray);
                if (recs?.Count > 0) return recs.Take(3).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RevenueForecast] AI recommendations failed for tenant {TenantId}", tenantId);
        }

        // Fallback rules-based recommendations
        var fallback = new List<string>();
        if (momGrowth < 0) fallback.Add("Launch a flash promotion this week to reverse the revenue decline.");
        else fallback.Add("Send a loyalty reward to your top 20% of clients to deepen retention.");
        if (members < 5) fallback.Add("Introduce a monthly membership plan to create predictable recurring revenue.");
        else fallback.Add("Upsell existing members to an annual membership for 2 months free.");
        fallback.Add("Use Fill My Calendar AI to fill open slots with lapsed clients via SMS.");
        return fallback;
    }
}

public class RevenueForecast
{
    public Guid TenantId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public decimal Last30DayRevenue { get; set; }
    public double MomGrowthPercent { get; set; }
    public decimal ConfirmedNextMonthRevenue { get; set; }
    public decimal MembershipMonthlyRevenue { get; set; }
    public decimal Forecast30Days { get; set; }
    public decimal Forecast60Days { get; set; }
    public decimal Forecast90Days { get; set; }
    public int ActiveMemberCount { get; set; }
    public List<string> AiRecommendations { get; set; } = new();
}
