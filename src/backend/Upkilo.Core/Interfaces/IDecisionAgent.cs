namespace Upkilo.Core.Interfaces;

public interface IDecisionAgent
{
    Task<string> AnalyzePerformanceAsync(Guid tenantId, DateTime from, DateTime to);
    Task<string> PredictChurnRiskAsync(Guid tenantId, Guid clientId);
    Task<string> GetGrowthRecommendationsAsync(Guid tenantId);
}
