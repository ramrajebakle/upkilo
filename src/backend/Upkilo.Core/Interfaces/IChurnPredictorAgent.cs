namespace Upkilo.Core.Interfaces;

public interface IChurnPredictorAgent
{
    Task<string> PredictChurnRiskAsync(Guid tenantId, Guid clientId);
    Task<string> GenerateRetentionStrategyAsync(Guid tenantId, Guid clientId);
}
