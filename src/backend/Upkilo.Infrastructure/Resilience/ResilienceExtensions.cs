using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;
using Microsoft.Extensions.Http.Resilience;

namespace Upkilo.Infrastructure.Resilience;

/// <summary>
/// Extension methods for configuring modern Polly v8 resilience pipelines
/// </summary>
public static class ResilienceExtensions
{
    private static readonly ResiliencePipeline<HttpResponseMessage> _standardPipeline;

    static ResilienceExtensions()
    {
        // Define a unified v8 pipeline
        _standardPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError || r.StatusCode == HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <summary>
    /// Add a resilient HttpClient using modern Polly v8 strategies
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
    {
        var builder = services.AddHttpClient(name);

        if (configureClient != null)
            builder.ConfigureHttpClient(configureClient);

        // Polly v8 integration for HttpClient
        builder.AddResilienceHandler("standard", pipeline =>
        {
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2)
            });
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            });
            pipeline.AddTimeout(TimeSpan.FromSeconds(30));
        });

        return builder;
    }
}

/// <summary>
/// Named HTTP client configuration for external services
/// </summary>
public static class HttpClientNames
{
    public const string Stripe = "Stripe";
    public const string SendGrid = "SendGrid";
    public const string Twilio = "Twilio";
    public const string GoogleCalendar = "GoogleCalendar";
    public const string Azure = "Azure";
    public const string OpenAI = "OpenAI";
}
