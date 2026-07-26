namespace Upkilo.Core.Interfaces;

/// <summary>
/// Resolves which AI model to use for a given tenant based on their subscription tier.
/// Cheap models (Haiku) for Free/Starter, full models (Sonnet) for Pro+.
/// This prevents Free-tier tenants from consuming expensive model capacity.
/// </summary>
public interface IAiModelResolver
{
    /// <summary>
    /// Returns the model identifier appropriate for the tenant's subscription tier.
    /// </summary>
    Task<string> ResolveAsync(Guid tenantId);

    /// <summary>
    /// Returns the model identifier for a known tier without a DB round-trip.
    /// </summary>
    string ResolveForTier(string tier);
}
