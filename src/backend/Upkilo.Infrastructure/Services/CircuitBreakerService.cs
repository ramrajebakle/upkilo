using System;
using Upkilo.Core.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Production-grade circuit breaker service leveraging Polly v8.
/// Managed named resilience pipelines for external API calls and critical paths.
/// </summary>
public class CircuitBreakerService
{
    private readonly ResiliencePipelineRegistry<string> _pipelineRegistry;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly IBusinessMetrics _metrics;

    public CircuitBreakerService(
        ResiliencePipelineRegistry<string> pipelineRegistry,
        ILogger<CircuitBreakerService> logger,
        IBusinessMetrics metrics)
    {
        _pipelineRegistry = pipelineRegistry;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Executes an action within a resilient pipeline.
    /// If no pipeline exists for the given name, it builds a default one.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> action, Func<Task<T>>? fallback = null)
    {
        var pipeline = _pipelineRegistry.GetOrAddPipeline<T>(circuitName, builder =>
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<T>().Handle<Exception>(),
                OnOpened = args =>
                {
                    _logger.LogWarning("Circuit {Name} opened for {Duration} due to {Reason}", 
                        circuitName, args.BreakDuration, args.Outcome.Exception?.Message);
                    _metrics.RecordCircuitBreakerTrip(circuitName);
                    return default;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit {Name} closed", circuitName);
                    return default;
                }
            });

            builder.AddTimeout(TimeSpan.FromSeconds(30));
        });

        try
        {
            return await pipeline.ExecuteAsync(async _ => await action());
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning("Circuit {Name} is open. Executing fallback if available.", circuitName);
            if (fallback != null) return await fallback();
            throw new CircuitBreakerOpenException($"Circuit {circuitName} is open.", ex);
        }
        catch (OperationCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Execution timed out in circuit {Name}. Executing fallback if available.", circuitName);
            if (fallback != null) return await fallback();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution failed in circuit {Name}", circuitName);
            if (fallback != null) return await fallback();
            throw;
        }
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message, Exception inner) : base(message, inner) { }
}
