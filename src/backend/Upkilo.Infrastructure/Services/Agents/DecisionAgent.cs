using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Agents;

public class DecisionAgent : IDecisionAgent
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;
    private readonly IAIDashboardService _dashboardService;

    public DecisionAgent(IAIService aiService, AppDbContext context, IAIDashboardService dashboardService)
    {
        _aiService = aiService;
        _context = context;
        _dashboardService = dashboardService;
    }

    public async Task<string> AnalyzePerformanceAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var bookingsCount = await _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.StartTime >= from && b.StartTime <= to);
        var revenue = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.StartTime >= from && b.StartTime <= to && b.Status == BookingStatus.Completed)
            .SumAsync(b => b.Price ?? 0m);

        var prompt = $"Act as a business consultant. Analyze the performance of a service business for the period {from:d} to {to:d}. " +
                     $"Data: Total Bookings: {bookingsCount}, Total Revenue: {revenue}. " +
                     "Provide a concise summary of performance and identify one area for improvement.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to analyze performance.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "DecisionAgent", "PerformanceAnalysis", prompt, content, 0.95m);
        }

        return content;
    }

    public async Task<string> PredictChurnRiskAsync(Guid tenantId, Guid clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return "Client not found.";

        var lastBooking = await _context.Bookings
            .Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.StartTime)
            .FirstOrDefaultAsync();

        var daysSinceLastVisit = lastBooking != null ? (DateTime.UtcNow - lastBooking.StartTime).TotalDays : 365;

        var prompt = $"Evaluate the churn risk for client {client.FirstName} {client.LastName}. " +
                     $"Last visit was {daysSinceLastVisit:F0} days ago. " +
                     "Predict if this client is at high, medium, or low risk of churn and suggest a re-engagement tactic.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt, "gpt-3.5-turbo");
        var content = result.Success ? result.Content ?? "" : "Failed to predict churn risk.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "DecisionAgent", "ChurnPrediction", prompt, content, 0.85m, entityId: clientId, entityType: "Client");
        }

        return content;
    }

    public async Task<string> GetGrowthRecommendationsAsync(Guid tenantId)
    {
        var topServices = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.Status == BookingStatus.Completed && b.Service != null)
            .GroupBy(b => b.Service!.Name)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToListAsync();

        var prompt = $"Based on the top performing services: {string.Join(", ", topServices)}, " +
                     "provide three tactical recommendations to grow this business. " +
                     "Focus on upselling, referrals, and seasonal promotions.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to generate growth recommendations.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "DecisionAgent", "GrowthRecommendation", prompt, content, 0.9m);
        }

        return content;
    }
}
