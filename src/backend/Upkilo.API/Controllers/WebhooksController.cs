using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.API.Middleware;
using Upkilo.API.Attributes;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("api_access")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        ITenantProvider tenantProvider,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Get all webhook endpoints
    /// </summary>
    [HttpGet("endpoints")]
    public async Task<IActionResult> GetEndpoints()
    {
        var endpoints = await _webhookService.GetEndpointsAsync(GetTenantId());
        return Ok(endpoints);
    }

    /// <summary>
    /// Create a new webhook endpoint
    /// </summary>
    [HttpPost("endpoints")]
    public async Task<IActionResult> CreateEndpoint([FromBody] CreateWebhookRequest request)
    {
        var webhook = await _webhookService.CreateEndpointAsync(
            GetTenantId(),
            request.Name,
            request.Url,
            request.Events
        );
        return CreatedAtAction(nameof(GetEndpoints), new { id = webhook.Id }, webhook);
    }

    /// <summary>
    /// Update a webhook endpoint
    /// </summary>
    [HttpPut("endpoints/{id}")]
    public async Task<IActionResult> UpdateEndpoint(Guid id, [FromBody] UpdateWebhookRequest request)
    {
        var success = await _webhookService.UpdateEndpointAsync(
            id, GetTenantId(), request.Name, request.Url, request.Events, request.IsActive
        );
        if (!success) return NotFound();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Delete a webhook endpoint
    /// </summary>
    [HttpDelete("endpoints/{id}")]
    public async Task<IActionResult> DeleteEndpoint(Guid id)
    {
        var success = await _webhookService.DeleteEndpointAsync(id, GetTenantId());
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Get webhook delivery logs
    /// </summary>
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveries([FromQuery] Guid? endpointId, [FromQuery] int limit = 20)
    {
        var deliveries = await _webhookService.GetDeliveriesAsync(GetTenantId(), endpointId, limit);
        return Ok(deliveries);
    }

    /// <summary>
    /// Resend a webhook delivery
    /// </summary>
    [HttpPost("deliveries/{id}/resend")]
    public async Task<IActionResult> ResendDelivery(Guid id)
    {
        var success = await _webhookService.ResendDeliveryAsync(id, GetTenantId());
        if (!success) return NotFound();
        return Ok(new { success = true, message = "Delivery resent" });
    }

    /// <summary>
    /// Clear webhook deliveries
    /// </summary>
    [HttpDelete("endpoints/{endpointId}/deliveries")]
    public async Task<IActionResult> ClearDeliveries(Guid endpointId)
    {
        var success = await _webhookService.ClearDeliveriesAsync(endpointId, GetTenantId());
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Send a test webhook event
    /// </summary>
    [HttpPost("endpoints/{id}/test")]
    public async Task<IActionResult> TestEndpoint(Guid id)
    {
        var delivery = await _webhookService.SendTestEventAsync(id, GetTenantId());
        return Ok(new { success = true, delivery, message = "Test event sent" });
    }

    /// <summary>
    /// Get available webhook event types
    /// </summary>
    [HttpGet("events")]
    public IActionResult GetEventTypes()
    {
        var events = new[]
        {
            new { Category = "Booking", Events = new[] { WebhookEvents.BookingCreated, WebhookEvents.BookingUpdated, WebhookEvents.BookingCancelled, WebhookEvents.BookingCompleted } },
            new { Category = "Client", Events = new[] { WebhookEvents.ClientCreated, WebhookEvents.ClientUpdated } },
            new { Category = "Payment", Events = new[] { WebhookEvents.PaymentReceived, WebhookEvents.PaymentFailed } },
            new { Category = "Invoice", Events = new[] { WebhookEvents.InvoiceCreated } },
            new { Category = "Staff", Events = new[] { WebhookEvents.StaffCreated } },
            new { Category = "Service", Events = new[] { WebhookEvents.ServiceCreated } },
            new { Category = "Reminder", Events = new[] { WebhookEvents.AppointmentReminder } }
        };
        return Ok(events);
    }
}

public class CreateWebhookRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string[] Events { get; set; } = Array.Empty<string>();
}

public class UpdateWebhookRequest
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string[]? Events { get; set; }
    public bool? IsActive { get; set; }
}

