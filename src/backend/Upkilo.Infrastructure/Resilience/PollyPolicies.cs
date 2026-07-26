using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Upkilo.Infrastructure.Resilience;

public static class PollyPolicies
{
    /// <summary>
    /// Standard retry policy: 3 retries with exponential backoff
    /// </summary>
    public static AsyncRetryPolicy CreateRetryPolicy(int retryCount = 3)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                });
    }

    /// <summary>
    /// Circuit breaker: Opens after 5 failures, stays open for 30 seconds
    /// </summary>
    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(
        int exceptionsAllowedBeforeBreaking = 5,
        int durationOfBreakSeconds = 30)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking,
                TimeSpan.FromSeconds(durationOfBreakSeconds),
                onBreak: (exception, duration) =>
                {
                    Console.WriteLine($"Circuit opened for {duration.TotalSeconds}s due to: {exception.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit closed - resuming normal operation");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("Circuit half-open - testing next call");
                });
    }

    /// <summary>
    /// Combined policy: Retry with Circuit Breaker
    /// </summary>
    public static IAsyncPolicy CreateResilientPolicy(
        int retryCount = 3,
        int exceptionsBeforeBreaking = 5,
        int breakDurationSeconds = 30)
    {
        var retryPolicy = CreateRetryPolicy(retryCount);
        var circuitBreaker = CreateCircuitBreakerPolicy(exceptionsBeforeBreaking, breakDurationSeconds);

        // Retry wraps circuit breaker - retries happen until circuit opens
        return Policy.WrapAsync(retryPolicy, circuitBreaker);
    }

    /// <summary>
    /// Timeout policy for external API calls
    /// </summary>
    public static AsyncPolicy CreateTimeoutPolicy(int timeoutSeconds = 30)
    {
        return Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds));
    }
}
