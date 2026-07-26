namespace Upkilo.Core.Interfaces;

public interface IBusinessMetrics
{
    void RecordBookingCreated(string tenantId, string serviceType);
    void RecordPaymentProcessed(string tenantId, decimal amount, string status);
    void RecordSmsSent(string tenantId);
    void RecordEmailSent(string tenantId);
    void RecordAiOperation(string tenantId, string operationType);
    void RecordBookingCancelled(string tenantId, string cancellationSource);
    void RecordCircuitBreakerTrip(string circuitName);
    void RecordDatabaseFailover();
    void RecordRegistrationAttempt(string outcome);
    void RecordCacheHit(string cacheName);
    void RecordCacheMiss(string cacheName);
    void RecordApiLatency(string tenantId, string endpoint, double durationMs);
    void RecordActiveConnection(int count);
}
