using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Net;

namespace Upkilo.Infrastructure;

/// <summary>
/// Polly v8 resilience policies for external service calls.
/// Provides retry with exponential backoff + circuit breaker patterns using modern ResiliencePipeline.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Gets a standard HTTP resilience pipeline with retry, circuit breaker, and timeout.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> GetHttpPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError || r.StatusCode == HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Polly] Retry {args.AttemptNumber} after {args.RetryDelay.TotalSeconds:F1}s — " +
                                      $"Status: {args.Outcome.Result?.StatusCode}");
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    Console.WriteLine($"[Polly] Circuit OPEN for {args.BreakDuration.TotalSeconds}s — " +
                                      $"Status: {args.Outcome.Result?.StatusCode}");
                    return default;
                },
                OnClosed = _ =>
                {
                    Console.WriteLine("[Polly] Circuit CLOSED — service recovered");
                    return default;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <summary>
    /// Gets a generic resilience pipeline for internal/SDK calls.
    /// </summary>
    public static ResiliencePipeline GetGenericPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(60)
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }
    /// <summary>
    /// Gets a generic retry-only pipeline (legacy compatibility).
    /// </summary>
    public static ResiliencePipeline GetGenericRetryPolicy() => GetGenericPipeline();

    /// <summary>
    /// Gets a generic circuit breaker pipeline (legacy compatibility).
    /// </summary>
    public static ResiliencePipeline GetGenericCircuitBreakerPolicy() => GetGenericPipeline();
}
