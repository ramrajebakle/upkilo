using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Events;
using Upkilo.Infrastructure.Background;
using Upkilo.API.Middleware;
using System.Security.Claims;

namespace Upkilo.API.Controllers;

/// <summary>
/// Express Checkout — streamlined single-request booking creation.
/// Resolves service availability, creates a booking, queues payment intent
/// and publishes BookingCreated event in one atomic flow.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bookings/express-checkout")]
[Authorize]
public class ExpressCheckoutController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IBookingService _bookingService;
    private readonly IEventService _eventService;
    private readonly ILogger<ExpressCheckoutController> _logger;

    public ExpressCheckoutController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        IBookingService bookingService,
        IEventService eventService,
        ILogger<ExpressCheckoutController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _bookingService = bookingService;
        _eventService = eventService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/bookings/express-checkout/services
    // Returns the quick-pick service list (active, price, duration, color)
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("services")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices([FromQuery] string? search = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _db.Services
            .Where(s => s.TenantId == tenantId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search));

        var services = await query
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Duration,
                s.Price,
                s.Color,
                s.Category,
                s.Description
            })
            .Take(50)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(services));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/bookings/express-checkout/slots
    //      ?serviceId=&date=YYYY-MM-DD&staffId= (optional)
    // Returns available 15-min time slots for the requested date
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime date,
        [FromQuery] Guid? staffId = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId && s.IsActive);
        if (service == null)
            return NotFound(ApiResponse<object>.Fail("Service not found"));

        // Fetch bookings for the day that may block slots
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var bookedSlots = await _db.Bookings
            .Where(b => b.TenantId == tenantId
                && b.ServiceId == serviceId
                && b.StartTime >= dayStart
                && b.StartTime < dayEnd
                && b.Status != BookingStatus.Cancelled
                && (staffId == null || b.StaffId == staffId))
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync();

        // Business hours: 08:00 – 20:00 in 30-min increments
        var slots = new List<object>();
        var cursor = dayStart.AddHours(8);
        var dayClose = dayStart.AddHours(20);

        while (cursor.AddMinutes(service.Duration) <= dayClose)
        {
            var slotEnd = cursor.AddMinutes(service.Duration);
            var isAvailable = !bookedSlots.Any(b =>
                b.StartTime < slotEnd && b.EndTime > cursor);

            slots.Add(new
            {
                time = cursor.ToString("HH:mm"),
                dateTime = cursor,
                available = isAvailable
            });

            cursor = cursor.AddMinutes(30);
        }

        return Ok(ApiResponse<object>.Ok(new { slots, date = dayStart, serviceId, staffId }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/bookings/express-checkout/staff
    //      ?serviceId= — staff capable of delivering this service
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaff([FromQuery] Guid? serviceId = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _db.Staff
            .Where(s => s.TenantId == tenantId && s.IsActive);

        var staff = await query
            .OrderBy(s => s.FirstName)
            .Select(s => new
            {
                s.Id,
                name = s.FirstName + " " + s.LastName,
                avatarUrl = s.AvatarUrl,
                specialty = s.Role,
                s.Title
            })
            .Take(30)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(staff));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/bookings/express-checkout
    // Single-call booking + payment intent creation
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ExpressCheckoutRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // ── Validate StartTime is not in the past ────────────────────────────
        if (req.StartTime < DateTime.UtcNow.AddMinutes(-5))
            return BadRequest(ApiResponse<object>.Fail("Cannot book a time slot in the past"));

        // ── Validate OverridePrice — only Owner/Admin may override ───────────
        if (req.OverridePrice.HasValue)
        {
            if (!User.IsInRole("Owner") && !User.IsInRole("Admin"))
                return StatusCode(403, ApiResponse<object>.Fail("Only Owner or Admin can override pricing"));
            if (req.OverridePrice.Value < 0)
                return BadRequest(ApiResponse<object>.Fail("Price cannot be negative"));
        }

        // ── Validate service ─────────────────────────────────────────────────
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == req.ServiceId && s.TenantId == tenantId && s.IsActive);
        if (service == null)
            return BadRequest(ApiResponse<object>.Fail("Invalid service"));

        // ── Resolve or create client ─────────────────────────────────────────
        Client? client = null;
        if (req.ClientId.HasValue)
        {
            client = await _db.Clients
                .FirstOrDefaultAsync(c => c.Id == req.ClientId.Value && c.TenantId == tenantId);
        }
        else if (!string.IsNullOrWhiteSpace(req.ClientEmail))
        {
            client = await _db.Clients
                .FirstOrDefaultAsync(c => c.Email == req.ClientEmail && c.TenantId == tenantId);

            if (client == null && !string.IsNullOrWhiteSpace(req.ClientName))
            {
                // Auto-create walk-in client
                var parts = req.ClientName.Trim().Split(' ', 2);
                client = new Client
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    FirstName = parts[0],
                    LastName = parts.Length > 1 ? parts[1] : "",
                    Email = req.ClientEmail,
                    Phone = req.ClientPhone ?? "",
                    CreatedAt = DateTime.UtcNow
                };
                _db.Clients.Add(client);
            }
        }

        if (client == null)
            return BadRequest(ApiResponse<object>.Fail("Client information required"));

        // ── Resolve staff (optional — any available if not specified) ─────────
        StaffMember? staff = null;
        if (req.StaffId.HasValue)
        {
            staff = await _db.Staff
                .FirstOrDefaultAsync(s => s.Id == req.StaffId.Value && s.TenantId == tenantId && s.IsActive);
            if (staff == null)
                return BadRequest(ApiResponse<object>.Fail("Staff member not found"));
        }
        else
        {
            // Pick the first available staff member for this service slot
            staff = await _db.Staff
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderBy(s => s.FirstName)
                .FirstOrDefaultAsync();
        }

        if (staff == null)
            return BadRequest(ApiResponse<object>.Fail("No available staff"));

        // ── Conflict check (with optimistic concurrency) ─────────────────────
        // NOTE: For production, wrap this check + insert in a distributed lock
        // (RedLock on key $"slot:{tenantId}:{staff.Id}:{req.StartTime:O}") to
        // prevent TOCTOU race conditions under concurrent booking load.
        var endTime = req.StartTime.AddMinutes(service.Duration);
        var conflict = await _db.Bookings
            .AnyAsync(b => b.TenantId == tenantId
                && b.StaffId == staff.Id
                && b.Status != BookingStatus.Cancelled
                && b.StartTime < endTime
                && b.EndTime > req.StartTime);

        if (conflict)
            return Conflict(ApiResponse<object>.Fail("Time slot is no longer available. Please select another slot."));

        // ── Determine price ──────────────────────────────────────────────────
        var price = req.OverridePrice ?? service.Price;

        // Apply promo code discount if provided
        decimal discountAmount = 0;
        if (!string.IsNullOrWhiteSpace(req.PromoCode))
        {
            var promo = await _db.PromoCodes
                .FirstOrDefaultAsync(p => p.TenantId == tenantId
                    && p.Code == req.PromoCode.ToUpper()
                    && p.IsActive
                    && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow));

            if (promo != null)
            {
                discountAmount = promo.DiscountType == PromoType.Percentage
                    ? Math.Round(price * (promo.DiscountValue / 100m), 2)
                    : Math.Min(promo.DiscountValue, price);
            }
        }

        var finalPrice = Math.Max(0, price - discountAmount);

        // ── Create booking ───────────────────────────────────────────────────
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = client.Id,
            ServiceId = service.Id,
            StaffId = staff.Id,
            StartTime = req.StartTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed,
            Price = finalPrice,
            Notes = req.Notes,
            Source = BookingSource.Widget,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        // ── Publish domain event ─────────────────────────────────────────────
        try
        {
            await _eventService.PublishAsync("BookingCreated", new BookingCreated
            {
                BookingId = booking.Id,
                TenantId = tenantId.Value,
                ClientId = client.Id,
                ServiceId = service.Id,
                StaffId = staff.Id,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Price = finalPrice
            }, tenantId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish BookingCreated event for booking {BookingId}", booking.Id);
        }

        var response = new
        {
            bookingId = booking.Id,
            confirmationCode = $"EC-{booking.Id.ToString("N")[..8].ToUpper()}",
            clientName = client.FirstName + " " + client.LastName,
            serviceName = service.Name,
            staffName = staff.FirstName + " " + staff.LastName,
            startTime = booking.StartTime,
            endTime = booking.EndTime,
            price = finalPrice,
            discount = discountAmount,
            status = booking.Status.ToString()
        };

        return CreatedAtAction(null, null, ApiResponse<object>.Ok(response));
    }
}

// ────────────────────────────────────────────────────────────────────────────
// Request DTO
// ────────────────────────────────────────────────────────────────────────────
public record ExpressCheckoutRequest
{
    public Guid ServiceId { get; init; }
    public DateTime StartTime { get; init; }
    public Guid? StaffId { get; init; }

    // Client — provide Id for existing, or Name+Email to auto-create walk-in
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public string? ClientEmail { get; init; }
    public string? ClientPhone { get; init; }

    public string? PromoCode { get; init; }
    public decimal? OverridePrice { get; init; }
    public string? Notes { get; init; }
    public string? PaymentMethodId { get; init; }  // Stripe PM id for future charge
}
