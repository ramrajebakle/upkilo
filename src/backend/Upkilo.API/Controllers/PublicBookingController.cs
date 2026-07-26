using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Memory;

namespace Upkilo.API.Controllers;

/// <summary>
/// Public booking page - no auth required
/// </summary>
[ApiController]
[Route("api/booking/{tenantSlug}")]
[EnableRateLimiting("public")]
public class PublicBookingController : ControllerBase
{
    private readonly ILogger<PublicBookingController> _logger;
    private readonly AppDbContext _context;
    private readonly ISchedulingService _schedulingService;
    private readonly IPaymentService _paymentService;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    private readonly IMemoryCache _cache;

    public PublicBookingController(
        ILogger<PublicBookingController> logger, 
        AppDbContext context, 
        ISchedulingService schedulingService,
        IPaymentService paymentService,
        IEventService eventService,
        IBookingService bookingService,
        IMemoryCache cache)
    {
        _logger = logger;
        _context = context;
        _schedulingService = schedulingService;
        _paymentService = paymentService;
        _eventService = eventService;
        _bookingService = bookingService;
        _cache = cache;
    }

    /// <summary>
    /// Get public booking page data
    /// </summary>
    /// <summary>
    /// Get public booking page data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBookingPage(string tenantSlug)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Locations)
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug);
            
        if (tenant == null) return NotFound("Business not found");

        var primaryLocation = tenant.Locations.FirstOrDefault(l => l.IsPrimary) ?? tenant.Locations.FirstOrDefault();

        return Ok(new
        {
            business = new
            {
                name = tenant.Name,
                slug = tenant.Slug,
                logo = tenant.LogoUrl ?? "/images/logo.png",
                primaryColor = tenant.PrimaryColor ?? "#06B6D4",
                description = tenant.Description ?? "Premium services",
                address = primaryLocation != null ? $"{primaryLocation.AddressLine1}, {primaryLocation.City}" : "Remote",
                phone = primaryLocation?.Phone ?? tenant.Phone,
                email = primaryLocation?.Email ?? tenant.Email
            },
            settings = new
            {
                allowGuestBooking = true,
                requireDeposit = false
            }
        });
    }

    /// <summary>
    /// Get available services for booking
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices(string tenantSlug)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        var services = await _context.Services
            .Where(s => s.TenantId == tenant.Id && s.IsActive)
            .Select(s => new {
                s.Id,
                s.Name,
                s.Description,
                s.DurationMinutes,
                s.Price,
                // Without this the booking clients had no way to know how to render Price and
                // fell back to a hardcoded symbol.
                Currency = s.Currency ?? tenant.Currency,
                s.Category,
                s.Color
            })
            .ToListAsync();

        return Ok(services);
    }

    /// <summary>
    /// Get staff members for a service
    /// </summary>
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff(string tenantSlug, [FromQuery] Guid? serviceId)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        var query = _context.StaffMembers
            .Where(s => s.TenantId == tenant.Id && s.IsActive);

        if (serviceId.HasValue)
        {
            query = query.Where(s => s.StaffServices.Any(ss => ss.ServiceId == serviceId.Value));
        }

        var staff = await query
            .Select(s => new
            {
                s.Id,
                name = s.FirstName + " " + s.LastName,
                title = s.Title,
                avatar = s.AvatarUrl ?? "/images/staff/default.jpg",
                rating = 5.0,
                reviewCount = 0
            })
            .ToListAsync();

        return Ok(staff);
    }

    /// <summary>
    /// Get available time slots
    /// </summary>
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        string tenantSlug,
        [FromQuery] Guid serviceId,
        [FromQuery] Guid? staffId,
        [FromQuery] DateTime date)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        string cacheKey = $"avail_{tenant.Id}_{serviceId}_{staffId}_{date:yyyyMMdd}";
        if (!_cache.TryGetValue(cacheKey, out List<DateTime>? slots) || slots == null)
        {
            var schedSlots = await _schedulingService.GetAvailableSlotsAsync(tenant.Id, serviceId, staffId, date);
            slots = schedSlots.ToList();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2)); // Short cache

            _cache.Set(cacheKey, slots, cacheOptions);
        }

        return Ok(new
        {
            date = date.Date,
            slots = slots.Select(s => new {
                time = s.ToString("HH:mm"),
                available = true,
                staffId = staffId
            }),
            waitlistAvailable = !slots.Any() // Allow waitlist if no slots
        });
    }

    /// <summary>
    /// Create a booking
    /// </summary>
    [HttpPost("book")]
    public async Task<IActionResult> CreateBooking(string tenantSlug, [FromBody] PublicBookingRequest request, [FromServices] ISubscriptionService subscriptionService)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.TenantId == tenant.Id);
        if (service == null) return BadRequest("Service not found");

        if (!await _schedulingService.CheckConcurrencyLimitAsync(tenant.Id))
        {
            return StatusCode(403, new { error = "This business has reached its concurrent booking limit. Please try again later." });
        }

        if (!await subscriptionService.CheckUsageLimitAsync(tenant.Id, UsageType.Bookings))
        {
            return StatusCode(429, new { message = "This business has reached its maximum monthly bookings limit." });
        }

        // Parse time
        if (!TimeSpan.TryParse(request.Time, out var startTimeSpan))
            return BadRequest("Invalid time format");

        var startTime = request.Date.Date.Add(startTimeSpan);
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // Verify availability (Safety check & Mass Assignment / DoS prevention)
        Guid? assignedStaffId = request.StaffId == Guid.Empty ? null : request.StaffId;

        if (assignedStaffId.HasValue)
        {
            var isAvailable = await _schedulingService.IsSlotAvailableAsync(tenant.Id, request.ServiceId, assignedStaffId.Value, startTime, service.DurationMinutes);
            if (!isAvailable) return BadRequest("Slot no longer available");
        }
        else 
        {
            // If no staff specified, we must verify that AT LEAST ONE staff member is available
            // and then ASSIGN that staff member to the booking to consume capacity!
            var staffIds = await _context.StaffServices
                    .Where(ss => ss.ServiceId == request.ServiceId)
                    .Select(ss => ss.StaffId)
                    .ToListAsync();
            
            bool foundAvailableStaff = false;
            foreach (var sId in staffIds)
            {
                if (await _schedulingService.IsSlotAvailableAsync(tenant.Id, request.ServiceId, sId, startTime, service.DurationMinutes))
                {
                    assignedStaffId = sId;
                    foundAvailableStaff = true;
                    break;
                }
            }
            if (!foundAvailableStaff)
            {
                return BadRequest("Slot no longer available");
            }
        }

        // Find or Create Client
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Email == request.Email);
        if (client == null)
        {
            client = new Client
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                CreatedAt = DateTime.UtcNow
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync(); // Save to get the ID

            // Dispatch Event
            await _eventService.PublishAsync(WebhookEvents.ClientCreated, client, tenant.Id);
        }

        // Determine status and handle deposit
        bool requiresPayment = service.RequiresPayment && service.DepositAmount > 0;
        var status = requiresPayment ? BookingStatus.Pending : BookingStatus.Confirmed;

        // Create Booking
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClientId = client.Id,
            ServiceId = request.ServiceId,
            StaffId = assignedStaffId,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            Price = service.Price,
            Notes = request.Notes,
            Source = string.Equals(request.UtmSource, "marketplace", StringComparison.OrdinalIgnoreCase)
                ? BookingSource.Marketplace
                : BookingSource.Website,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        await subscriptionService.IncrementUsageAsync(tenant.Id, UsageType.Bookings);
        
        _logger.LogInformation("Booking created: {BookingId} for {Email}. Status: {Status}", booking.Id, request.Email, status);

        // Dispatch Event
        await _eventService.PublishAsync(WebhookEvents.BookingCreated, new
        {
            BookingId = booking.Id,
            client.Email,
            client.FirstName,
            client.LastName,
            service.Name,
            booking.StartTime,
            booking.Status
        }, tenant.Id);

        string? clientSecret = null;
        if (requiresPayment)
        {
            // Determine Stripe Connect application fee:
            // - Marketplace bookings: 10% Upkilo commission (always applied)
            // - Tenant-enabled processing fee: 0.5-1% opt-in fee (from settings)
            long? applicationFeeAmount = null;
            if (!string.IsNullOrEmpty(tenant.StripeConnectId))
            {
                double feePercent = 0.0;

                if (booking.Source == BookingSource.Marketplace)
                {
                    feePercent = 10.0; // 10% marketplace commission
                }
                else if (tenant.Settings.TryGetValue("platform_fee_enabled", out var feeEnabled)
                    && feeEnabled is bool b && b)
                {
                    feePercent = tenant.Settings.TryGetValue("platform_fee_percent", out var pctVal)
                        && pctVal is double d ? d : 0.5;
                }

                if (feePercent > 0)
                    applicationFeeAmount = (long)Math.Round((service.DepositAmount ?? 0) * (decimal)(feePercent / 100.0) * 100);
            }

            var paymentRequest = new CreatePaymentRequest(
                tenant.Id,
                booking.Id,
                service.DepositAmount ?? 0,
                service.Currency,
                $"Deposit for {service.Name} booking",
                true, // Capture immediately
                applicationFeeAmount,
                tenant.StripeConnectId
            );

            var paymentResult = await _paymentService.CreatePaymentIntentAsync(paymentRequest);
            if (paymentResult.Success)
            {
                clientSecret = paymentResult.ClientSecret;
            }
            else
            {
                _logger.LogError("Failed to create Stripe payment intent for booking {BookingId}: {Error}", booking.Id, paymentResult.Error);
                // We might want to handle this more gracefully, but for now we'll return the error
                return StatusCode(500, new { message = "Payment system error. Please try again later." });
            }
        }

        return Ok(new
        {
            id = booking.Id,
            confirmationNumber = $"BK-{booking.Id.ToString()[..8].ToUpper()}",
            status = booking.Status.ToString().ToLower(),
            message = requiresPayment ? "Please complete payment to confirm your booking." : "Your booking has been confirmed.",
            requiresPayment,
            clientSecret,
            booking = new
            {
                id = booking.Id,
                serviceName = service.Name,
                startTime = booking.StartTime,
                endTime = booking.EndTime,
                clientName = $"{client.FirstName} {client.LastName}",
                clientEmail = client.Email,
                depositAmount = service.DepositAmount
            }
        });
    }

    /// <summary>
    /// Join the waitlist
    /// </summary>
    [HttpPost("waitlist")]
    public async Task<IActionResult> JoinWaitlist(string tenantSlug, [FromBody] WaitlistRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        var entry = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ServiceId = request.ServiceId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            PreferredDate = request.Date.Date,
            PreferredTimeRange = request.TimeRange,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        return Ok(new { message = "You have been added to the waitlist." });
    }

    /// <summary>
    /// Check booking status
    /// </summary>
    [HttpGet("status/{id}")]
    public async Task<IActionResult> GetBookingStatus(string tenantSlug, Guid id)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Include(b => b.Client)
                .Include(b => b.Tenant)
                    .ThenInclude(t => t.Locations)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenant.Id);

        if (booking == null) return NotFound();

        var primaryLocation = tenant.Locations.FirstOrDefault(l => l.IsPrimary) ?? tenant.Locations.FirstOrDefault();

        return Ok(new
        {
            confirmationNumber = $"BK-{booking.Id.ToString()[..8].ToUpper()}",
            serviceId = booking.ServiceId,
            staffId = booking.StaffId,
            status = booking.Status.ToString().ToLower(),
            service = booking.Service?.Name,
            date = booking.StartTime.Date,
            time = booking.StartTime.ToString("HH:mm"),
            staff = booking.Staff != null ? $"{booking.Staff.FirstName} {booking.Staff.LastName}" : "Any Professional",
            location = primaryLocation?.Name ?? tenant.Name,
            address = primaryLocation != null ? $"{primaryLocation.AddressLine1}, {primaryLocation.City}" : null,
            canCancel = CanCancel(booking, tenant, out _),
            canReschedule = CanReschedule(booking, tenant)
        });
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    [HttpPost("cancel/{id}")]
    public async Task<IActionResult> CancelBooking(string tenantSlug, Guid id, [FromBody] CancelBookingRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenant.Id);
            
        if (booking == null) return NotFound();

        if (!CanCancel(booking, tenant, out var penaltyPercent))
            return BadRequest("Cancellation is not allowed for this booking based on the policy.");

        // Process refund if deposit was paid
        if (booking.DepositPaid > 0 && booking.Payments.Any())
        {
            var successfulPayment = booking.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Succeeded);
            if (successfulPayment != null && !string.IsNullOrEmpty(successfulPayment.StripePaymentIntentId))
            {
                decimal refundRatio = 1m - (penaltyPercent / 100m);
                if (refundRatio > 0)
                {
                    decimal refundAmount = booking.DepositPaid * refundRatio;
                    var refundResult = await _paymentService.RefundPaymentAsync(new RefundRequest(
                        successfulPayment.StripePaymentIntentId,
                        refundAmount,
                        "Booking cancellation"
                    ), tenant.Id);

                    if (refundResult.Success)
                    {
                        successfulPayment.RefundAmount = refundAmount;
                        successfulPayment.Status = refundRatio == 1m ? PaymentStatus.Refunded : PaymentStatus.Partial;
                        _logger.LogInformation("Refunded {Amount} for booking {BookingId}", refundAmount, booking.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to refund booking {BookingId}: {Error}", booking.Id, refundResult.Error);
                    }
                }
            }
        }

        booking.Status = BookingStatus.Cancelled;
        // Optionally save reason if we had a column/json for it
        await _context.SaveChangesAsync();

        // Dispatch Event
        await _eventService.PublishAsync(WebhookEvents.BookingCancelled, new
        {
            BookingId = booking.Id,
            booking.StartTime,
            Reason = "User requested via public portal"
        }, tenant.Id);

        return Ok(new { message = "Booking cancelled successfully" });
    }

    /// <summary>
    /// Reschedule a booking
    /// </summary>
    [HttpPost("reschedule/{id}")]
    public async Task<IActionResult> RescheduleBooking(string tenantSlug, Guid id, [FromBody] RescheduleRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug);
        if (tenant == null) return NotFound();

        try
        {
            // Parse time
            if (!TimeSpan.TryParse(request.Time, out var startTimeSpan))
                return BadRequest(new { error = "Invalid time format" });

            var newStartTime = request.Date.Date.Add(startTimeSpan);

            var booking = await _bookingService.RescheduleBookingAsync(tenant.Id, id, newStartTime, request.ConfirmationCode);

            return Ok(new { 
                message = "Booking rescheduled successfully", 
                newTime = booking.StartTime,
                confirmationNumber = booking.Id.ToString().Substring(0, 8).ToUpper()
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Public reschedule failed for booking {BookingId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred while rescheduling." });
        }
    }

    private bool CanCancel(Booking booking, Tenant tenant, out decimal penaltyPercent)
    {
        penaltyPercent = 0m;
        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed) return false;

        // Default: 24h
        int noticeHours = 24;
        bool allowCancel = true;
        decimal latePenalty = 100m; // Default: 100% of deposit is kept if cancelled late
        bool allowLateCancel = false;

        if (tenant.Settings.TryGetValue("booking_notice_period_hours", out var hoursObj) && int.TryParse(hoursObj.ToString(), out var h))
            noticeHours = h;
            
        if (tenant.Settings.TryGetValue("booking_allow_cancel", out var allowObj) && bool.TryParse(allowObj.ToString(), out var a))
            allowCancel = a;
            
        if (tenant.Settings.TryGetValue("booking_late_cancel_allow", out var lateAllowObj) && bool.TryParse(lateAllowObj.ToString(), out var la))
            allowLateCancel = la;

        if (tenant.Settings.TryGetValue("booking_late_cancel_penalty_percent", out var penObj) && decimal.TryParse(penObj.ToString(), out var pen))
            latePenalty = pen;

        if (!allowCancel) return false;

        var hoursUntil = (booking.StartTime - DateTime.UtcNow).TotalHours;
        
        if (hoursUntil > noticeHours)
        {
            penaltyPercent = 0m; // Inside free cancellation window
            return true;
        }

        if (allowLateCancel)
        {
            penaltyPercent = latePenalty; // Late cancellation with penalty
            return true;
        }

        return false;
    }

    private bool CanReschedule(Booking booking, Tenant tenant)
    {
        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed) return false;

        // Default: 24h
        int noticeHours = 24;
        bool allowReschedule = true;

        if (tenant.Settings.TryGetValue("booking_notice_period_hours", out var hoursObj) && int.TryParse(hoursObj.ToString(), out var h))
        {
            noticeHours = h;
        }
        if (tenant.Settings.TryGetValue("booking_allow_reschedule", out var allowObj) && bool.TryParse(allowObj.ToString(), out var a))
        {
            allowReschedule = a;
        }

        if (!allowReschedule) return false;

        return booking.StartTime > DateTime.UtcNow.AddHours(noticeHours);
    }
}

public record RescheduleRequest(DateTime Date, string Time, string? ConfirmationCode = null);

public record PublicBookingRequest(
    Guid ServiceId,
    Guid? StaffId,
    DateTime Date,
    string Time,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Notes,
    string? UtmSource = null    // "marketplace" triggers 10% Upkilo commission via Stripe Connect
);

public record WaitlistRequest(
    Guid ServiceId,
    DateTime Date,
    string TimeRange,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Notes
);

