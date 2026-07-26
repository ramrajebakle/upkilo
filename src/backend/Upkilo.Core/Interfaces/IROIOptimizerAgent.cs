namespace Upkilo.Core.Interfaces;

public interface IROIOptimizerAgent
{
    Task<string> AnalyzeCampaignROIAsync(Guid tenantId, Guid campaignId);
    Task<string> SuggestBudgetAllocationAsync(Guid tenantId, decimal totalBudget);
}
