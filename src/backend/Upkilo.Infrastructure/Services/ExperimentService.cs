using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implements Task 1458: A/B Testing framework
/// Implements Task 1365: Chaos engineering tests (Simulated)
/// </summary>
public class ExperimentService
{
    private readonly ILogger<ExperimentService> _logger;
    private static readonly ConcurrentDictionary<string, string> _assignedVariants = new();

    public ExperimentService(ILogger<ExperimentService> logger)
    {
        _logger = logger;
    }

    public string GetVariant(Guid tenantId, string experimentKey, Guid userId)
    {
        var key = $"{tenantId}:{experimentKey}:{userId}";
        return _assignedVariants.GetOrAdd(key, k => 
        {
            var hash = k.GetHashCode();
            return hash % 2 == 0 ? "A" : "B";
        });
    }

    public async Task SimulateChaosAsync(string component)
    {
        _logger.LogWarning("Task 1365: Injecting Chaos Engineering into {Component}...", component);
        
        // Simulating random failure for testing (5% chance)
        if (Random.Shared.Next(1, 100) <= 5)
        {
            _logger.LogCritical("Chaos Engineering: Injected failure in {Component}!", component);
            throw new Exception($"Chaos Engineering Failure Injected: {component}");
        }

        await Task.CompletedTask;
    }
}
