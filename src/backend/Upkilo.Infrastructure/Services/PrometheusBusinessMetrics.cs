using Prometheus;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

public class PrometheusBusinessMetrics : IBusinessMetrics
{
    private static readonly Counter BookingsCounter = Metrics.CreateCounter(
        "upkilo_bookings_total", "Total number of bookings created.",
        new CounterConfiguration { LabelNames = new[] { "tenant_id", "service_type" } });

    private static readonly Counter PaymentsCounter = Metrics.CreateCounter(
        "upkilo_payments_total", "Total number of payments processed.",
        new CounterConfiguration { LabelNames = new[] { "tenant_id", "status" } });

    private static readonly Counter SmsCounter = Metrics.CreateCounter(
        "upkilo_sms_sent_total", "Total number of SMS sent.",
        new CounterConfiguration { LabelNames = new[] { "tenant_id" } });

    private static readonly Counter EmailCounter = Metrics.CreateCounter(
        "upkilo_emails_sent_total", "Total number of emails sent.",
        new CounterConfiguration { LabelNames = new[] { "tenant_id" } });

    private static readonly Counter AiCounter = Metrics.CreateCounter(
        "upkilo_ai_operations_total", "Total number of AI operations performed.",
        new CounterConfiguration { LabelNames = new[] { "tenant_id", "operation_type" } });

    private static readonly Counter CancellationsCounter = Metrics.CreateCounter(
        "upkilo_bookings_cancelled_total", "Total number of bookings cancelled.",
        new CounterConfiguration { LabelNames = new[] { "tenant_id", "source" } });

    private static readonly Counter CircuitBreakerCounter = Metrics.CreateCounter(
        "upkilo_circuit_trips_total", "Total number of times a circuit breaker tripped.",
        new CounterConfiguration { LabelNames = new[] { "circuit_name" } });

    private static readonly Counter FailoverCounter = Metrics.CreateCounter(
        "upkilo_db_failover_total", "Total number of database failover events.");

    private static readonly Counter RegistrationCounter = Metrics.CreateCounter(
        "upkilo_registration_attempts_total", "Total number of registration attempts.",
        new CounterConfiguration { LabelNames = new[] { "outcome" } });

    private static readonly Counter CacheHitCounter = Metrics.CreateCounter(
        "upkilo_cache_hits_total", "Total number of cache hits.",
        new CounterConfiguration { LabelNames = new[] { "cache_name" } });

    private static readonly Counter CacheMissCounter = Metrics.CreateCounter(
        "upkilo_cache_misses_total", "Total number of cache misses.",
        new CounterConfiguration { LabelNames = new[] { "cache_name" } });

    private static readonly Histogram ApiLatencyHistogram = Metrics.CreateHistogram(
        "upkilo_api_latency_seconds", "API request latency in seconds.",
        new HistogramConfiguration
        {
            LabelNames = new[] { "tenant_id", "endpoint" },
            Buckets = new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 }
        });

    private static readonly Gauge ActiveConnectionsGauge = Metrics.CreateGauge(
        "upkilo_active_connections", "Current number of active client connections.");

    public void RecordBookingCreated(string tenantId, string serviceType) =>
        BookingsCounter.WithLabels(tenantId, serviceType).Inc();

    public void RecordPaymentProcessed(string tenantId, decimal amount, string status) =>
        PaymentsCounter.WithLabels(tenantId, status).Inc();

    public void RecordSmsSent(string tenantId) =>
        SmsCounter.WithLabels(tenantId).Inc();

    public void RecordEmailSent(string tenantId) =>
        EmailCounter.WithLabels(tenantId).Inc();

    public void RecordAiOperation(string tenantId, string operationType) =>
        AiCounter.WithLabels(tenantId, operationType).Inc();

    public void RecordBookingCancelled(string tenantId, string cancellationSource) =>
        CancellationsCounter.WithLabels(tenantId, cancellationSource).Inc();

    public void RecordCircuitBreakerTrip(string circuitName) =>
        CircuitBreakerCounter.WithLabels(circuitName).Inc();

    public void RecordDatabaseFailover() =>
        FailoverCounter.Inc();

    public void RecordRegistrationAttempt(string outcome) =>
        RegistrationCounter.WithLabels(outcome).Inc();

    public void RecordCacheHit(string cacheName) =>
        CacheHitCounter.WithLabels(cacheName).Inc();

    public void RecordCacheMiss(string cacheName) =>
        CacheMissCounter.WithLabels(cacheName).Inc();

    public void RecordApiLatency(string tenantId, string endpoint, double durationMs) =>
        ApiLatencyHistogram.WithLabels(tenantId, endpoint).Observe(durationMs / 1000.0);

    public void RecordActiveConnection(int count) =>
        ActiveConnectionsGauge.Set(count);
}
