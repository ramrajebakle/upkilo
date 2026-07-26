using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Agents;

public class ROIOptimizerAgent : IROIOptimizerAgent
{
    private readonly IAIService _aiService;
    private readonly AppDbContext _context;
    private readonly IAIDashboardService _dashboardService;

    public ROIOptimizerAgent(IAIService aiService, AppDbContext context, IAIDashboardService dashboardService)
    {
        _aiService = aiService;
        _context = context;
        _dashboardService = dashboardService;
    }

    public async Task<string> AnalyzeCampaignROIAsync(Guid tenantId, Guid campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.TenantId != tenantId) return "Campaign not found.";

        var prompt = $"Analyze the ROI for this marketing campaign: Name: {campaign.Name}, Type: {campaign.Type}, Status: {campaign.Status}. " +
                     "Provide a breakdown of how to optimize the return on investment based on standard metrics for this type of campaign.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to analyze ROI.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "ROIOptimizerAgent", "CampaignROI", prompt, content, 0.9m, "gpt-4", 0, 0, campaignId, "Campaign");
        }

        return content;
    }

    public async Task<string> SuggestBudgetAllocationAsync(Guid tenantId, decimal totalBudget)
    {
        var prompt = $"Given a total marketing budget of {totalBudget:C}, " +
                     "suggest an optimal budget allocation across channels (e.g. Meta Ads, Google Ads, Email marketing, SMS) " +
                     "to maximize ROI for a typical local service business.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        var content = result.Success ? result.Content ?? "" : "Failed to suggest budget allocation.";

        if (result.Success)
        {
            await _dashboardService.LogDecisionAsync(tenantId, "ROIOptimizerAgent", "BudgetAllocation", prompt, content, 0.85m);
        }

        return content;
    }
}
