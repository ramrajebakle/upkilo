using System.Diagnostics;
using Prometheus;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Middleware;

/// <summary>
/// Custom Prometheus metrics middleware that tracks HTTP request duration,
/// status codes, and endpoint-level latency for observability dashboards.
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Histogram HttpRequestDuration = Metrics.CreateHistogram(
        "upkilo_http_request_duration_seconds",
        "Duration of HTTP requests in seconds.",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status_code", "tenant_id" },
            Buckets = new[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
        });

    private static readonly Counter HttpRequestTotal = Metrics.CreateCounter(
        "upkilo_http_requests_total",
        "Total number of HTTP requests.",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status_code", "tenant_id" }
        });

    private static readonly Gauge HttpRequestsInFlight = Metrics.CreateGauge(
        "upkilo_http_requests_in_flight",
        "Number of HTTP requests currently being processed.");

    private static readonly Counter HttpRequestErrors = Metrics.CreateCounter(
        "upkilo_http_request_errors_total",
        "Total number of HTTP request errors (5xx status codes).",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint" }
        });

    private readonly IBusinessMetrics _businessMetrics;

    public MetricsMiddleware(RequestDelegate next, IBusinessMetrics businessMetrics)
    {
        _next = next;
        _businessMetrics = businessMetrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip metrics endpoint itself to avoid recursion
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        HttpRequestsInFlight.Inc();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            HttpRequestsInFlight.Dec();

            var method = context.Request.Method;
            var endpoint = GetNormalizedEndpoint(context);
            var statusCode = context.Response.StatusCode.ToString();
            var elapsed = sw.Elapsed.TotalSeconds;
            var tenantId = context.Items["TenantId"]?.ToString() ?? "unknown";

            HttpRequestDuration.WithLabels(method, endpoint, statusCode, tenantId).Observe(elapsed);
            HttpRequestTotal.WithLabels(method, endpoint, statusCode, tenantId).Inc();

            // Record in business metrics too for tenant-specific dashboards
            _businessMetrics.RecordApiLatency(tenantId, endpoint, sw.Elapsed.TotalMilliseconds);
            _businessMetrics.RecordActiveConnection((int)HttpRequestsInFlight.Value);

            if (context.Response.StatusCode >= 500)
            {
                HttpRequestErrors.WithLabels(method, endpoint).Inc();
            }
        }
    }

    /// <summary>
    /// Normalizes endpoints to prevent high cardinality from route parameters (e.g., GUIDs).
    /// Converts /api/v1/bookings/abc-123 → /api/v1/bookings/{id}
    /// </summary>
    private static string GetNormalizedEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "/";
        }
        return context.Request.Path.Value ?? "/";
    }
}
