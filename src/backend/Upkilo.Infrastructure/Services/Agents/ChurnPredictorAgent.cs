using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Agents;

public class ChurnPredictorAgent : IChurnPredictorAgent
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;
    private readonly IAIDashboardService _dashboardService;

    public ChurnPredictorAgent(IAIService aiService, AppDbContext context, IAIDashboardService dashboardService)
    {
        _aiService = aiService;
        _context = context;
        _dashboardService = dashboardService;
    }

    public async Task<string> PredictChurnRiskAsync(Guid tenantId, Guid clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null || client.TenantId != tenantId) return "Client not found.";

        var lastBooking = await _context.Bookings
            .Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.StartTime)
            .FirstOrDefaultAsync();

        var daysSinceLastVisit = lastBooking != null ? (DateTime.UtcNow - lastBooking.StartTime).TotalDays : 365;

        var prompt = $"Evaluate the churn risk for client {client.FirstName} {client.LastName}. " +
                     $"Last visit was {daysSinceLastVisit:F0} days ago. " +
                     "Predict if this client is at high, medium, or low risk of churn based on standard service business retention patterns.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt, "gpt-3.5-turbo");
        var content = result.Success ? result.Content ?? "" : "Failed to predict churn risk.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "ChurnPredictorAgent", "ChurnPrediction", prompt, content, 0.85m, "gpt-3.5-turbo", 0, 0, clientId, "Client");
        }

        return content;
    }

    public async Task<string> GenerateRetentionStrategyAsync(Guid tenantId, Guid clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null || client.TenantId != tenantId) return "Client not found.";

        var prompt = $"Generate a personalized retention strategy and marketing message for client {client.FirstName} {client.LastName} " +
                     "who is at risk of churning. Suggest a specific promotion or follow-up tactic to win them back.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to generate retention strategy.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "ChurnPredictorAgent", "RetentionStrategy", prompt, content, 0.8m, "gpt-4", 0, 0, clientId, "Client");
        }

        return content;
    }
}
