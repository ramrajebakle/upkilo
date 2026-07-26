using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Attributes;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/zapier")]
[Authorize]
[FeatureGuard("api_access")]
public class ZapierController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ITenantProvider _tenantProvider;
    private readonly AppDbContext _context;
    private readonly ILogger<ZapierController> _logger;

    public ZapierController(
        IWebhookService webhookService,
        ITenantProvider tenantProvider,
        AppDbContext context,
        ILogger<ZapierController> logger)
    {
        _webhookService = webhookService;
        _tenantProvider = tenantProvider;
        _context = context;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() 
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Auth test for Zapier. Returns tenant and user info.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> AuthTest()
    {
        var tenantId = GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        var userId = _tenantProvider.GetUserId() ?? Guid.Empty;
        var user = await _context.Users.FindAsync(userId);

        return Ok(new
        {
            tenantName = tenant?.Name,
            userName = $"{user?.FirstName} {user?.LastName}",
            userEmail = user?.Email,
            tenantId = tenantId
        });
    }

    /// <summary>
    /// Subscribe to a REST hook (Zapier Trigger)
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] ZapierSubscribeRequest request)
    {
        var webhook = await _webhookService.CreateEndpointAsync(
            GetTenantId(),
            $"Zapier: {request.Event}",
            request.TargetUrl,
            new[] { request.Event }
        );
        return CreatedAtAction(nameof(AuthTest), new { id = webhook.Id }, new { id = webhook.Id });
    }

    /// <summary>
    /// Unsubscribe from a REST hook
    /// </summary>
    [HttpDelete("unsubscribe/{id}")]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var success = await _webhookService.DeleteEndpointAsync(id, GetTenantId());
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Sample data for Zapier triggers (polling for discovery)
    /// </summary>
    [HttpGet("perform-list")]
    public async Task<IActionResult> PerformList([FromQuery] string @event)
    {
        var tenantId = GetTenantId();
        
        if (@event == WebhookEvents.BookingCreated || @event == "booking.created")
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Service)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new { 
                    b.Id, 
                    startTime = b.StartTime, 
                    endTime = b.EndTime,
                    status = b.Status.ToString(),
                    price = b.Price,
                    clientName = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Unknown",
                    serviceName = b.Service != null ? b.Service.Name : "Unknown"
                })
                .ToListAsync();
            return Ok(bookings);
        }
        
        if (@event == WebhookEvents.ClientCreated || @event == "client.created")
        {
            var clients = await _context.Clients
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.Phone,
                    c.CreatedAt
                })
                .ToListAsync();
            return Ok(clients);
        }

        return Ok(new List<object>());
    }

    /// <summary>
    /// Action: Create a new client
    /// </summary>
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] ZapierCreateClientRequest request)
    {
        var tenantId = GetTenantId();
        
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = request.FirstName,
            LastName = request.LastName ?? string.Empty,
            Email = request.Email ?? string.Empty,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(AuthTest), new { id = client.Id }, client);
    }

    /// <summary>
    /// Search for a client by email
    /// </summary>
    [HttpGet("clients/search")]
    public async Task<IActionResult> SearchClient([FromQuery] string email)
    {
        var tenantId = GetTenantId();
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == email && c.TenantId == tenantId);
        
        if (client == null) return Ok(new List<Client>());
        return Ok(new[] { client });
    }

    /// <summary>
    /// Action: Create a booking
    /// </summary>
    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] ZapierCreateBookingRequest request)
    {
        var tenantId = GetTenantId();
        
        var service = await _context.Services.FindAsync(request.ServiceId);
        if (service == null) return BadRequest("Service not found");

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = BookingStatus.Confirmed,
            Price = service.Price,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(AuthTest), new { id = booking.Id }, booking);
    }
}

public record ZapierSubscribeRequest(string TargetUrl, string Event);

public record ZapierCreateClientRequest(
    string FirstName,
    string? LastName,
    string? Email,
    string? Phone
);

public record ZapierCreateBookingRequest(
    Guid ClientId,
    Guid ServiceId,
    Guid StaffId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
);
