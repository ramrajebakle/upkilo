using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Hubs;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Client check-in kiosk — simplified tablet/iPad interface for arrivals.
/// Designed for lobby kiosks with minimal authentication requirements.
/// VULN-A11: Both endpoints are AllowAnonymous; rate-limited to 20 req/min per IP
/// to prevent enumeration of client PII (name/email/phone) across the tenant.
/// </summary>
// SEC-01 FIX: this controller was previously [AllowAnonymous] and scoped every query by a
// caller-supplied `tenantId` — letting anyone with a tenant's GUID enumerate that tenant's client
// PII and manipulate bookings (rate-limiting alone did not close this). The kiosk is operated by a
// logged-in staff member (the dashboard kiosk page is behind auth), so we now require [Authorize]
// and derive the tenant STRICTLY from the authenticated token — never from client input.
[ApiController]
[Route("api/v1/kiosk")]
[Authorize]
[EnableRateLimiting("kiosk")]
public class KioskController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub, INotificationClient> _hub;
    private readonly ILogger<KioskController> _logger;
    private readonly ITenantProvider _tenantProvider;

    public KioskController(
        AppDbContext context,
        IHubContext<NotificationHub, INotificationClient> hub,
        ILogger<KioskController> logger,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _hub = hub;
        _logger = logger;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Search for today's bookings by phone, email, or client name.
    /// Used by the kiosk to find the client's appointment.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchBookings(
        [FromQuery] string? phone,
        [FromQuery] string? email,
        [FromQuery] string? name)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Provide phone, email, or name to search" });

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = _context.Set<Booking>()
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.TenantId == tenantId
                        && b.StartTime >= today
                        && b.StartTime < tomorrow
                        && b.Status != BookingStatus.Cancelled
                        && b.CheckedInAt == null);

        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(b => b.Client != null && b.Client.Phone == phone);
        else if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(b => b.Client != null && b.Client.Email == email);
        else if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(b => b.Client != null &&
                (b.Client.FirstName + " " + b.Client.LastName).Contains(name));

        var bookings = await query
            .OrderBy(b => b.StartTime)
            .Take(10)
            .Select(b => new
            {
                b.Id,
                b.StartTime,
                b.EndTime,
                b.Status,
                b.GroupSize,
                Service = b.Service == null ? null : new { b.Service.Name, b.Service.DurationMinutes },
                Staff = b.Staff == null ? null : new { b.Staff.FirstName, b.Staff.LastName },
                Client = b.Client == null ? null : new { b.Client.FirstName, b.Client.LastName }
            })
            .ToListAsync();

        return Ok(new { results = bookings, count = bookings.Count });
    }

    /// <summary>
    /// Check in a client for their booking.
    /// Sets CheckedInAt timestamp and notifies staff via SignalR.
    /// </summary>
    [HttpPost("check-in/{bookingId:guid}")]
    public async Task<IActionResult> CheckIn(Guid bookingId)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var booking = await _context.Set<Booking>()
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);

        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        if (booking.CheckedInAt != null)
            return Conflict(new { error = "Already checked in", checkedInAt = booking.CheckedInAt });

        if (booking.Status == BookingStatus.Cancelled)
            return BadRequest(new { error = "Cannot check in to a cancelled booking" });

        // Perform check-in
        booking.CheckedInAt = DateTime.UtcNow;
        if (booking.Status == BookingStatus.Confirmed)
            booking.Status = BookingStatus.InProgress;

        await _context.SaveChangesAsync();

        // Notify staff dashboard in real-time
        await _hub.Clients.Group($"tenant_{tenantId}").NewClientArrival(new ClientArrivalNotification(
            ClientId: booking.ClientId?.ToString() ?? "",
            ClientName: booking.Client != null
                ? $"{booking.Client.FirstName} {booking.Client.LastName}"
                : "Walk-in",
            BookingId: booking.Id.ToString(),
            ServiceName: booking.Service?.Name ?? "Unknown",
            ArrivalTime: booking.CheckedInAt ?? DateTime.UtcNow
        ));

        _logger.LogInformation("Client checked in for booking {BookingId} at tenant {TenantId}",
            bookingId, tenantId);

        return Ok(new
        {
            success = true,
            message = "Checked in successfully!",
            booking = new
            {
                booking.Id,
                booking.StartTime,
                service = booking.Service?.Name,
                staff = booking.Staff != null
                    ? $"{booking.Staff.FirstName} {booking.Staff.LastName}"
                    : null,
                checkedInAt = booking.CheckedInAt
            }
        });
    }

    /// <summary>
    /// Register a walk-in booking directly from the kiosk.
    /// Creates a new booking with IsWalkIn=true and immediately checks in.
    /// </summary>
    [HttpPost("walk-in")]
    public async Task<IActionResult> WalkIn([FromBody] WalkInRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var service = await _context.Set<Service>()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.TenantId == tenantId);

        if (service == null)
            return NotFound(new { error = "Service not found" });

        // Find or create client
        Client? client = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            client = await _context.Set<Client>()
                .FirstOrDefaultAsync(c => c.Phone == request.Phone && c.TenantId == tenantId);
        }

        if (client == null && !string.IsNullOrWhiteSpace(request.FirstName))
        {
            client = new Client
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = request.FirstName,
                LastName = request.LastName ?? "",
                Phone = request.Phone,
                Email = request.Email,
                Source = "kiosk"
            };
            _context.Set<Client>().Add(client);
        }

        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client?.Id,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            StartTime = now,
            EndTime = now.AddMinutes(service.DurationMinutes),
            Status = BookingStatus.InProgress,
            Source = BookingSource.Manual,
            IsWalkIn = true,
            CheckedInAt = now,
            Price = service.Price,
            GroupSize = request.GroupSize ?? 1
        };

        _context.Set<Booking>().Add(booking);
        await _context.SaveChangesAsync();

        // Notify staff
        await _hub.Clients.Group($"tenant_{tenantId}").NewClientArrival(new ClientArrivalNotification(
            ClientId: client?.Id.ToString() ?? "",
            ClientName: client != null ? $"{client.FirstName} {client.LastName}" : "Walk-in Guest",
            BookingId: booking.Id.ToString(),
            ServiceName: service.Name,
            ArrivalTime: now
        ));

        _logger.LogInformation("Walk-in booking {BookingId} created at tenant {TenantId}",
            booking.Id, tenantId);

        return Ok(new
        {
            success = true,
            booking = new
            {
                booking.Id,
                booking.StartTime,
                booking.EndTime,
                service = service.Name,
                groupSize = booking.GroupSize
            }
        });
    }

    /// <summary>
    /// Get available services for today (kiosk service picker).
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var services = await _context.Set<Service>()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Category,
                s.DurationMinutes,
                s.Price,
                s.Currency,
                s.MaxAttendees,
                s.Color
            })
            .ToListAsync();

        return Ok(new { services });
    }

    /// <summary>
    /// Get estimated wait time for a checked-in booking.
    /// </summary>
    [HttpGet("wait-time/{bookingId:guid}")]
    public async Task<IActionResult> GetWaitTime(Guid bookingId)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized();

        var booking = await _context.Set<Booking>()
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);

        if (booking == null) return NotFound(new { error = "Booking not found" });

        if (booking.Status == BookingStatus.InProgress)
        {
            return Ok(new { waitTimeMinutes = 0, message = "It's your turn!" });
        }

        if (booking.CheckedInAt == null)
        {
            return BadRequest(new { error = "Client is not checked in yet" });
        }

        // Extremely simple estimation: if booking start time is in the future, wait time is the difference
        var now = DateTime.UtcNow;
        var waitTime = booking.StartTime > now ? (int)(booking.StartTime - now).TotalMinutes : 0;

        // Better estimation: check for earlier bookings with the same staff that are still in progress or checked in
        if (booking.StaffId.HasValue)
        {
            var priorBookings = await _context.Set<Booking>()
                .Where(b => b.TenantId == tenantId && b.StaffId == booking.StaffId && b.StartTime < booking.StartTime && b.Status != BookingStatus.Completed && b.Status != BookingStatus.Cancelled)
                .OrderByDescending(b => b.EndTime)
                .FirstOrDefaultAsync();

            if (priorBookings != null)
            {
                waitTime = Math.Max(waitTime, (int)(priorBookings.EndTime - now).TotalMinutes);
            }
        }

        return Ok(new { waitTimeMinutes = waitTime > 0 ? waitTime : 0 });
    }
}

public class WalkInRequest
{
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? StaffId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? GroupSize { get; set; }
}
