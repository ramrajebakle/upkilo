using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IWebhookService
{
    // Endpoint management
    Task<Webhook> CreateEndpointAsync(Guid tenantId, string name, string url, string[] events);
    Task<IEnumerable<Webhook>> GetEndpointsAsync(Guid tenantId);
    Task<Webhook?> GetEndpointAsync(Guid id);
    Task<bool> DeleteEndpointAsync(Guid id, Guid tenantId);
    Task<bool> UpdateEndpointAsync(Guid id, Guid tenantId, string? name, string? url, string[]? events, bool? isActive);
    
    // Event dispatch
    Task DispatchEventAsync(Guid tenantId, string eventType, object payload);
    Task<WebhookDelivery> SendTestEventAsync(Guid endpointId, Guid tenantId);
    
    // Delivery logs
    Task<IEnumerable<WebhookDelivery>> GetDeliveriesAsync(Guid tenantId, Guid? endpointId = null, int limit = 50);
    Task<bool> ResendDeliveryAsync(Guid deliveryId, Guid tenantId);
    Task<bool> ClearDeliveriesAsync(Guid endpointId, Guid tenantId);
    
    // Background processing
    Task ProcessPendingDeliveriesAsync();

    // Generic HTTP Request for Workflow Automation Actions
    Task<bool> SendWebhookRequestAsync(string url, string method, object payload, Dictionary<string, string>? headers = null);
}

public static class WebhookEvents
{
    public const string BookingCreated = "booking.created";
    public const string BookingUpdated = "booking.updated";
    public const string BookingCancelled = "booking.cancelled";
    public const string BookingCompleted = "booking.completed";
    public const string ClientCreated = "client.created";
    public const string ClientUpdated = "client.updated";
    public const string PaymentReceived = "payment.received";
    public const string PaymentFailed = "payment.failed";
    public const string InvoiceCreated = "invoice.created";
    public const string StaffCreated = "staff.created";
    public const string ServiceCreated = "service.created";
    public const string AppointmentReminder = "appointment.reminder";
    
    public static readonly string[] All = new[]
    {
        BookingCreated, BookingUpdated, BookingCancelled, BookingCompleted,
        ClientCreated, ClientUpdated, PaymentReceived, PaymentFailed,
        InvoiceCreated, StaffCreated, ServiceCreated, AppointmentReminder
    };
}

public interface IWebhookV2Service
{
    Task PublishEventAsync<T>(string eventType, T payload, Guid? tenantId = null) where T : class;
    Task<bool> VerifySignatureAsync(string payload, string signature, string secret);
    Task ProcessRetriesAsync();
}
