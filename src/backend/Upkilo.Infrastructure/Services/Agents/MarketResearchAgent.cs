using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Agents;

public class MarketResearchAgent : IMarketResearchAgent
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;
    private readonly IAIDashboardService _dashboardService;

    public MarketResearchAgent(IAIService aiService, AppDbContext context, IAIDashboardService dashboardService)
    {
        _aiService = aiService;
        _context = context;
        _dashboardService = dashboardService;
    }

    public async Task<string> AnalyzeLocalCompetitorsAsync(Guid tenantId, string industry, string location)
    {
        var prompt = $"Act as a market researcher. Analyze the local competitive landscape for a {industry} business located in {location}. " +
                     "Identify 3-5 typical competitor types, their common strategies, and suggest 3 differentiation tactics our business can use to stand out.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to analyze competitors.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "MarketResearchAgent", "CompetitorAnalysis", prompt, content, 0.85m);
        }

        return content;
    }

    public async Task<string> SuggestPricingStrategyAsync(Guid tenantId, string serviceName)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        var industryString = tenant?.BusinessType.ToString() ?? "service";

        var prompt = $"Act as a pricing strategy expert. Suggest an optimal pricing strategy for the service '{serviceName}' " +
                     $"in the {industryString} industry. Consider value-based pricing, tiered options, and psychological pricing techniques.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to suggest pricing strategy.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "MarketResearchAgent", "PricingStrategy", prompt, content, 0.9m);
        }

        return content;
    }
}
