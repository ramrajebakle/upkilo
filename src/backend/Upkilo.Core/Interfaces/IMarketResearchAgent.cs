namespace Upkilo.Core.Interfaces;

public interface IMarketResearchAgent
{
    Task<string> AnalyzeLocalCompetitorsAsync(Guid tenantId, string industry, string location);
    Task<string> SuggestPricingStrategyAsync(Guid tenantId, string serviceName);
}
