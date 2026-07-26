using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Registry for managing and discovering available webhook events.
/// </summary>
public class WebhookRegistry
{
    private readonly List<WebhookEventDefinition> _events = new();

    public WebhookRegistry()
    {
        // Core Events
        Register("client.created", "Triggered when a new client profile is created.");
        Register("client.updated", "Triggered when client information is updated.");
        Register("client.deleted", "Triggered when a client is deleted.");

        // Booking Events
        Register("booking.created", "Triggered when a new booking is made.");
        Register("booking.confirmed", "Triggered when a booking is confirmed.");
        Register("booking.cancelled", "Triggered when a booking is cancelled.");
        Register("booking.rescheduled", "Triggered when a booking date/time is changed.");

        // Billing Events
        Register("invoice.created", "Triggered when a new invoice is generated.");
        Register("payment.succeeded", "Triggered when a payment is successful.");
        Register("payment.failed", "Triggered when a payment attempt fails.");
    }

    public void Register(string eventType, string description)
    {
        if (!_events.Any(e => e.EventType == eventType))
        {
            _events.Add(new WebhookEventDefinition(eventType, description));
        }
    }

    public IEnumerable<WebhookEventDefinition> GetAll() => _events;

    public bool IsValid(string eventType) => eventType == "*" || _events.Any(e => e.EventType == eventType);
}

public record WebhookEventDefinition(string EventType, string Description);
