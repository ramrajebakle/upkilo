using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using MediatR;
using Upkilo.Core.Events;
using Upkilo.Infrastructure.Background;

using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

/// <summary>
/// Bookings controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly ILogger<BookingsController> _logger;
    private readonly IEventService _eventService;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISchedulingService _schedulingService;
    private readonly IBookingService _bookingService;
    private readonly IMediator _mediator;

    public BookingsController(
        ILogger<BookingsController> logger,
        IEventService eventService,
        AppDbContext context,
        ITenantProvider tenantProvider,
        ISchedulingService schedulingService,
        IBookingService bookingService,
        IMediator mediator)
    {
        _logger = logger;
        _eventService = eventService;
        _context = context;
        _tenantProvider = tenantProvider;
        _schedulingService = schedulingService;
        _bookingService = bookingService;
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a paginated list of bookings
    /// </summary>
    /// <response code="200">Returns the list of bookings</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookings(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        // H-05 FIX: Explicitly filter by tenant to prevent data leak when
        // tenant provider returns null (e.g., SuperAdmin JWT without tenant_id).
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // M-6 FIX: Clamp pagination parameters
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);
        var query = _context.Bookings.Where(b => b.TenantId == tenantId).AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }
            else
            {
                query = query.Where(b => false);
            }
        }

        if (startDate.HasValue)
            query = query.Where(b => b.StartTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(b => b.StartTime <= endDate.Value);

        var total = await query.CountAsync();
        var bookings = await query
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .OrderByDescending(b => b.StartTime)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(b => new
            {
                b.Id,
                clientName = b.Client != null ? b.Client.FirstName + " " + b.Client.LastName : "Unknown",
                clientEmail = b.Client != null ? b.Client.Email : string.Empty,
                serviceName = b.Service != null ? b.Service.Name : "Unknown",
                staffName = b.Staff != null ? b.Staff.FirstName + " " + b.Staff.LastName : "Unassigned",
                b.StartTime,
                b.EndTime,
                b.Status,
                b.Price,
                b.GroupSize
            })
            .ToListAsync();

        return Ok(new
        {
            data = bookings,
            page,
            limit,
            total
        });
    }

    /// <summary>
    /// Gets a specific booking by ID
    /// </summary>
    /// <response code="200">Returns the booking</response>
    /// <response code="404">Booking not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var booking = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);

        if (booking == null) return NotFound();

        return Ok(new
        {
            booking.Id,
            clientName = booking.Client != null ? booking.Client.FirstName + " " + booking.Client.LastName : string.Empty,
            clientEmail = booking.Client?.Email,
            clientPhone = booking.Client?.Phone,
            serviceName = booking.Service?.Name ?? string.Empty,
            booking.ServiceId,
            staffName = booking.Staff != null ? booking.Staff.FirstName + " " + booking.Staff.LastName : string.Empty,
            booking.StaffId,
            booking.StartTime,
            booking.EndTime,
            booking.Status,
            booking.Price,
            booking.GroupSize,
            booking.Notes,
            booking.CreatedAt,
            booking.RowVersion,
            booking.Version
        });
    }

    /// <summary>
    /// Create booking
    /// </summary>
    [HttpPost]
    [ChecksUsage(UsageType.Bookings)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        try
        {
            // C-2 FIX: Delegate to BookingService which wraps the operation in a
            // transaction with pessimistic locking, slot hold validation, and
            // availability verification — preventing double-bookings.
            var model = new CreateBookingModel(
                ClientId: request.ClientId,
                ServiceId: request.ServiceId,
                StaffId: request.StaffId,
                StartTime: request.StartTime,
                EndTime: request.EndTime,
                Notes: request.Notes,
                GroupSize: request.GroupSize
            );

            var booking = await _bookingService.CreateBookingAsync(tenantId.Value, model);

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBooking(Guid id, [FromBody] UpdateBookingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        try
        {
            // If only status or notes are changing, use the service which handles concurrency
            if (!request.StartTime.HasValue && !request.EndTime.HasValue && !request.StaffId.HasValue && request.Status.HasValue)
            {
                var updated = await _bookingService.UpdateStatusAsync(tenantId.Value, id, request.Status.Value, request.Notes, request.RowVersion);
                return Ok(updated);
            }

            // Fallback for full edits (should ideally also be moved to service in the future)
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
            if (booking == null) return NotFound();

            if (request.RowVersion != null)
            {
                _context.Entry(booking).Property(b => b.RowVersion).OriginalValue = request.RowVersion;
            }

            if (request.StartTime.HasValue) booking.StartTime = request.StartTime.Value;
            if (request.EndTime.HasValue) booking.EndTime = request.EndTime.Value;
            if (request.StaffId.HasValue) booking.StaffId = request.StaffId.Value;
            if (request.Status.HasValue) booking.Status = request.Status.Value;
            if (request.Notes != null) booking.Notes = request.Notes;

            booking.UpdatedAt = DateTime.UtcNow;
            booking.Version++;

            await _context.SaveChangesAsync();
            return Ok(booking);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "This booking was modified by another user. Please refresh." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk cancel bookings
    /// </summary>
    [HttpPost("bulk-cancel")]
    public async Task<IActionResult> BulkCancelBookings(
        [FromBody] BulkCancelRequest request,
        [FromServices] Upkilo.Infrastructure.Services.WaitlistAutoFillService waitlistFill)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // L-2 FIX: Validate list size to prevent unbounded queries
        if (request.BookingIds == null || request.BookingIds.Count == 0)
            return BadRequest(new { error = "No booking IDs provided." });
        if (request.BookingIds.Count > 500)
            return BadRequest(new { error = "Cannot cancel more than 500 bookings at once." });

        var bookingsToCancel = await _context.Bookings
            .Where(b => b.TenantId == tenantId && request.BookingIds.Contains(b.Id) && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        foreach (var booking in bookingsToCancel)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.Notes = request.Reason ?? booking.Notes;
            booking.UpdatedAt = DateTime.UtcNow;
        }

        if (bookingsToCancel.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Bulk cancelled {Count} bookings", bookingsToCancel.Count);

            // H-6 FIX: Publish events AFTER SaveChanges to ensure data consistency
            foreach (var booking in bookingsToCancel)
            {
                await _eventService.PublishAsync("booking.cancelled", booking, tenantId.Value);
            }

            // Trigger waitlist promotion + real-time SMS+email notification for each freed slot
            foreach (var booking in bookingsToCancel)
            {
                await ProcessWaitlistPromotion(booking);
                if (booking.ServiceId.HasValue)
                    await waitlistFill.NotifyNextOnWaitlistAsync(tenantId.Value, booking.ServiceId.Value, booking.StaffId, booking.StartTime);
            }
        }

        return Ok(new { success = true, cancelledCount = bookingsToCancel.Count });
    }

    /// <summary>
    /// Export bookings to CSV
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportBookings()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // M-3 FIX: Use Select() projection instead of Include() to reduce memory
        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.StartTime)
            .Take(1000) // Limit for sync export
            .Select(b => new
            {
                b.Id,
                ClientName = b.Client != null ? b.Client.FirstName + " " + b.Client.LastName : "Unknown",
                ServiceName = b.Service != null ? b.Service.Name : "Unknown",
                StaffName = b.Staff != null ? b.Staff.FirstName + " " + b.Staff.LastName : "Unassigned",
                b.StartTime,
                b.EndTime,
                b.Status,
                b.Price
            })
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Booking ID,Client,Service,Staff,Start,End,Status,Price");

        foreach (var b in bookings)
        {
            // C-6 FIX: Sanitize fields to prevent CSV formula injection
            csv.AppendLine($"{b.Id},{SanitizeCsvField(b.ClientName)},{SanitizeCsvField(b.ServiceName)},{SanitizeCsvField(b.StaffName)},{b.StartTime:O},{b.EndTime:O},{b.Status},{b.Price}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"bookings_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    /// <summary>
    /// C-6 FIX: Prevent CSV formula injection by escaping dangerous leading characters.
    /// </summary>
    private static string SanitizeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // Escape fields containing commas, quotes, or newlines
        var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');

        // Prefix formula-injection characters with a single quote
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
        {
            value = "'" + value;
        }

        // Escape internal quotes and wrap in quotes if needed
        value = value.Replace("\"", "\"\"");
        return needsQuoting ? $"\"{value}\"" : value;
    }

    /// <summary>
    /// Create walk-in booking
    /// </summary>
    [HttpPost("walk-in")]
    [ChecksUsage(UsageType.Bookings)]
    public async Task<IActionResult> CreateWalkInBooking([FromBody] CreateWalkInRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!await _schedulingService.CheckConcurrencyLimitAsync(tenantId.Value))
        {
            return StatusCode(403, new { error = "Tenant has reached the maximum number of concurrent bookings allowed by their subscription tier." });
        }

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId);
        if (service == null) return BadRequest("Service not found");

        Guid? staffId = request.StaffId;

        // L-1 FIX: Auto-assign to least busy active staff instead of first found
        if (!staffId.HasValue)
        {
            var now = DateTime.UtcNow;
            var availableStaff = await _context.Set<StaffMember>()
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderBy(s => _context.Bookings.Count(b => b.StaffId == s.Id && b.Status != BookingStatus.Cancelled && b.EndTime > now))
                .FirstOrDefaultAsync();

            if (availableStaff != null)
                staffId = availableStaff.Id;
        }

        var startTime = request.StartTime ?? DateTime.UtcNow;
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // H-02 FIX: Delegate walk-in booking creation to BookingService
        // to inherit pessimistic locking, transaction wrapping, and slot checks.
        var model = new CreateBookingModel(
            ClientId: request.ClientId,
            ServiceId: request.ServiceId,
            StaffId: staffId.Value,
            StartTime: startTime,
            EndTime: endTime,
            Notes: request.Notes,
            GroupSize: request.GroupSize,
            IsWalkIn: true
        );

        var booking = await _bookingService.CreateBookingAsync(tenantId.Value, model);

        return Ok(new
        {
            id = booking.Id,
            status = booking.Status.ToString()
        });
    }

    /// <summary>
    /// Check-in existing booking
    /// </summary>
    [HttpPut("{id}/check-in")]
    public async Task<IActionResult> CheckInBooking(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (booking == null) return NotFound();

        booking.CheckedInAt = DateTime.UtcNow;
        booking.Status = BookingStatus.InProgress;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("booking.checked_in", booking, tenantId.Value);

        _logger.LogInformation("Booking {BookingId} checked in", id);

        return Ok(new { success = true, checkedInAt = booking.CheckedInAt });
    }

    /// <summary>
    /// Get today's walk-ins
    /// </summary>
    [HttpGet("walk-ins")]
    public async Task<IActionResult> GetWalkIns()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var walkIns = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.TenantId == tenantId &&
                        b.IsWalkIn &&
                        b.StartTime >= today &&
                        b.StartTime < tomorrow)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                ClientName = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Unknown",
                Phone = b.Client != null ? b.Client.Phone : null,
                ServiceName = b.Service != null ? b.Service.Name : "Unknown",
                StaffName = b.Staff != null ? $"{b.Staff.FirstName} {b.Staff.LastName}" : "Unassigned",
                b.StartTime,
                b.Status,
                b.CheckedInAt,
                b.Price
            })
            .ToListAsync();

        return Ok(new { data = walkIns, count = walkIns.Count });
    }

    [HttpPost("recurring")]
    [ChecksUsage(UsageType.Bookings)]
    public async Task<IActionResult> CreateRecurringBooking([FromBody] CreateRecurringBookingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var model = new CreateRecurringBookingModel(
            request.ClientId,
            request.ServiceId,
            request.StaffId,
            request.StartDate,
            request.Frequency,
            request.Interval,
            request.DaysOfWeek,
            request.EndDate,
            request.Occurrences,
            request.StartTime,
            request.Notes,
            request.GroupSize
        );

        try
        {
            var result = await _bookingService.CreateRecurringBookingAsync(tenantId.Value, model);

            if (result.SuccessCount == 0)
            {
                return BadRequest(new
                {
                    error = "None of the requested time slots are available.",
                    conflicts = result.ConflictedDates
                });
            }

            return Ok(new
            {
                patternId = result.PatternId,
                successCount = result.SuccessCount,
                conflictCount = result.ConflictCount,
                successfulDates = result.SuccessfulDates,
                conflictedDates = result.ConflictedDates
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    /// <summary>
    /// Bulk update booking status
    /// </summary>
    [HttpPost("bulk-update-status")]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkStatusUpdateRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // L-2 FIX: Validate list size
        if (request.Ids == null || request.Ids.Count == 0)
            return BadRequest(new { error = "No booking IDs provided." });
        if (request.Ids.Count > 500)
            return BadRequest(new { error = "Cannot update more than 500 bookings at once." });

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId && request.Ids.Contains(b.Id))
            .ToListAsync();

        foreach (var booking in bookings)
        {
            // H-03 FIX: Validate state transitions — cannot transition FROM terminal states
            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            {
                _logger.LogWarning("Skipping booking {BookingId}: cannot change status from terminal state {Status}", booking.Id, booking.Status);
                continue;
            }
            booking.Status = request.Status;
            booking.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // H-6 FIX: Publish events AFTER successful SaveChanges
        foreach (var booking in bookings)
        {
            await _eventService.PublishAsync("booking.status_updated", booking, tenantId.Value);
        }

        // Day 48: Award loyalty points when bookings are marked complete
        if (request.Status == BookingStatus.Completed)
        {
            var loyaltySvc = HttpContext.RequestServices.GetService<Upkilo.Infrastructure.Services.LoyaltyService>();
            if (loyaltySvc != null)
            {
                foreach (var booking in bookings.Where(b => b.ClientId.HasValue && b.Status == BookingStatus.Completed))
                {
                    try
                    {
                        var isFirst = !await _context.Bookings.AnyAsync(
                            other => other.ClientId == booking.ClientId &&
                                     other.Status == BookingStatus.Completed &&
                                     other.Id != booking.Id);
                        await loyaltySvc.AwardBookingPointsAsync(booking.ClientId!.Value, booking.Price ?? 0, booking.Id, isFirst);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Loyalty] Failed to award points for booking {Id}", booking.Id);
                    }
                }
            }
        }

        _logger.LogInformation("Bulk updated {Count} bookings to status {Status}", bookings.Count, request.Status);

        return Ok(new { success = true, count = bookings.Count });
    }

    /// <summary>
    /// Get booking history/timeline
    /// </summary>
    [HttpGet("{id}/timeline")]
    public async Task<IActionResult> GetBookingTimeline(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var activities = await _context.AuditEntries
            .Where(a => a.TenantId == tenantId && a.EntityType == "Booking" && a.EntityId == id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return Ok(activities);
    }

    /// <summary>
    /// Reconcile booking payments (Verify if paid in full)
    /// </summary>
    [HttpGet("{id}/reconcile")]
    public async Task<IActionResult> ReconcileBooking(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var booking = await _context.Bookings
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);

        if (booking == null) return NotFound();

        var totalPaid = booking.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .Sum(p => p.Amount);

        var balance = (booking.Price ?? 0) - totalPaid;

        return Ok(new
        {
            bookingId = id,
            totalPrice = booking.Price,
            totalPaid,
            balance,
            isReconciled = balance <= 0,
            paymentStatus = booking.PaymentStatus
        });
    }

    /// <summary>
    /// Reconcile all bookings for a tenant (Find discrepancies)
    /// </summary>
    [HttpGet("reconcile-all")]
    public async Task<IActionResult> ReconcileAll([FromQuery] DateTime? startDate = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var date = startDate ?? DateTime.UtcNow.AddDays(-30);

        var bookedItems = await _context.Bookings
            .Include(b => b.Payments)
            .Where(b => b.TenantId == tenantId && b.StartTime >= date && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var discrepancies = bookedItems
            .Select(b => new
            {
                b.Id,
                b.StartTime,
                Expected = b.Price ?? 0,
                Paid = b.Payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount)
            })
            .Where(x => x.Expected > x.Paid)
            .ToList();

        return Ok(new { count = discrepancies.Count, discrepancies });
    }

    /// <summary>GET /api/v1/bookings/conflicts — detect double-booked staff or overlapping time slots</summary>
    [HttpGet("conflicts")]
    public async Task<IActionResult> DetectConflicts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Query-string dates bind with Kind=Unspecified, which Npgsql refuses to write to a
        // 'timestamp with time zone' column — the request then failed with HTTP 400. The
        // defaults are already UTC; normalise anything supplied by the caller.
        var start = ToUtc(from) ?? DateTime.UtcNow.AddDays(-7);
        var end = ToUtc(to) ?? DateTime.UtcNow.AddDays(30);

        var bookings = await _context.Bookings
            .Where(b => b.TenantId == tenantId
                && b.Status != BookingStatus.Cancelled
                && b.StartTime >= start && b.StartTime <= end)
            .OrderBy(b => b.StaffId).ThenBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.StaffId,
                b.StartTime,
                b.EndTime,
                b.ClientId,
                clientName = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Unknown",
                serviceName = b.Service != null ? b.Service.Name : "Unknown",
                staffName = b.Staff != null ? $"{b.Staff.FirstName} {b.Staff.LastName}" : "Unknown",
                b.Status,
            })
            .ToListAsync();

        var conflicts = new List<object>();
        for (int i = 0; i < bookings.Count; i++)
        {
            for (int j = i + 1; j < bookings.Count; j++)
            {
                var a = bookings[i];
                var b = bookings[j];
                // Same staff, overlapping times
                if (a.StaffId == b.StaffId && a.StaffId != null)
                {
                    var aEnd = a.EndTime;
                    var bEnd = b.EndTime;
                    if (a.StartTime < bEnd && aEnd > b.StartTime)
                    {
                        conflicts.Add(new
                        {
                            type = "staff_double_booking",
                            bookingA = new { a.Id, a.clientName, a.serviceName, a.staffName, a.StartTime, endTime = aEnd },
                            bookingB = new { b.Id, b.clientName, b.serviceName, b.staffName, b.StartTime, endTime = bEnd },
                            overlapMinutes = (int)(Math.Min(aEnd.Ticks, bEnd.Ticks) - Math.Max(a.StartTime.Ticks, b.StartTime.Ticks)) / TimeSpan.TicksPerMinute,
                        });
                    }
                }
            }
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            conflicts,
            totalConflicts = conflicts.Count,
            scannedBookings = bookings.Count,
            dateRange = new { from = start, to = end },
        }));
    }

    /// <summary>POST /api/v1/bookings/{id}/resolve-conflict — resolve a booking conflict</summary>
    [HttpPost("{id}/resolve-conflict")]
    public async Task<IActionResult> ResolveConflict(Guid id, [FromBody] ResolveConflictRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);

        if (booking == null) return NotFound();

        switch (request.Resolution)
        {
            case "reschedule":
                if (request.NewStartTime.HasValue)
                {
                    // H-04 FIX: Check availability before rescheduling to prevent
                    // creating new double-bookings while resolving old ones.
                    var duration = booking.EndTime - booking.StartTime;
                    var durationMinutes = (int)duration.TotalMinutes;
                    bool isAvailable = await _schedulingService.IsSlotAvailableAsync(
                        tenantId.Value, booking.ServiceId ?? Guid.Empty, booking.StaffId,
                        request.NewStartTime.Value, durationMinutes > 0 ? durationMinutes : 60);
                    if (!isAvailable)
                    {
                        return Conflict(new { error = "The new time slot is not available. Please choose another time." });
                    }
                    booking.StartTime = request.NewStartTime.Value;
                    booking.EndTime = request.NewStartTime.Value.Add(duration);
                }
                break;
            case "cancel":
                booking.Status = BookingStatus.Cancelled;
                break;
            case "reassign_staff":
                if (request.NewStaffId.HasValue)
                    booking.StaffId = request.NewStaffId.Value;
                break;
        }

        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { id, resolution = request.Resolution }));
    }

    private async Task ProcessWaitlistPromotion(Booking booking)
    {
        try
        {
            var nextEntry = await _context.WaitlistEntries
                .Where(w => w.TenantId == booking.TenantId
                            && w.ServiceId == booking.ServiceId
                            && w.Status == WaitlistStatus.Waiting
                            && (w.PreferredDate == DateTime.MinValue || w.PreferredDate.Date == booking.StartTime.Date))
                .OrderByDescending(w => w.Priority)
                .ThenBy(w => w.CreatedAt)
                .FirstOrDefaultAsync();

            if (nextEntry != null)
            {
                _logger.LogInformation("Auto-promoting waitlist entry {EntryId} for cancelled booking {BookingId}", nextEntry.Id, booking.Id);

                // Notify them via Event Service
                await _eventService.PublishAsync("waitlist.auto_promotion", new
                {
                    nextEntry.Id,
                    nextEntry.Email,
                    nextEntry.FirstName,
                    bookingId = booking.Id,
                    availableStartTime = booking.StartTime,
                    availableEndTime = booking.EndTime
                }, booking.TenantId);

                nextEntry.Status = WaitlistStatus.Notified;
                nextEntry.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing waitlist promotion for booking {BookingId}", booking.Id);
        }
    }

    /// <summary>
    /// Normalise a model-bound DateTime to UTC. Query-string values bind with
    /// Kind=Unspecified, and Npgsql rejects those for 'timestamp with time zone' columns.
    /// Values already marked Utc pass through; Local values are converted.
    /// </summary>
    private static DateTime? ToUtc(DateTime? value) => value is null
        ? null
        : value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
}

public record BulkStatusUpdateRequest(List<Guid> Ids, BookingStatus Status);

// L-6 FIX: Use enum instead of magic strings for conflict resolution
public enum ConflictResolution
{
    Reschedule,
    Cancel,
    ReassignStaff
}

public class ResolveConflictRequest
{
    public string Resolution { get; set; } = string.Empty; // "reschedule", "cancel", "reassign_staff"
    public DateTime? NewStartTime { get; set; }
    public Guid? NewStaffId { get; set; }
}


public record CreateBookingRequest(
    Guid? ClientId,
    Guid ServiceId,
    Guid StaffId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes,
    string? ClientEmail,
    string? ClientName,
    string? ClientPhone,
    int GroupSize = 1
);

public record UpdateBookingRequest(
    DateTime? StartTime,
    DateTime? EndTime,
    Guid? StaffId,
    BookingStatus? Status,
    string? Notes,
    byte[]? RowVersion
);

public record CancelBookingRequest(string? Reason);

public record CreateWalkInRequest(
    Guid? ClientId,
    Guid ServiceId,
    Guid? StaffId = null,
    DateTime? StartTime = null,
    string? Notes = null,
    int GroupSize = 1
);


public record CreateRecurringBookingRequest(
    Guid? ClientId,
    Guid ServiceId,
    Guid StaffId,
    DateTime StartDate,
    string Frequency, // "Daily", "Weekly", "Monthly"
    int Interval, // e.g. 1 means every week
    List<int>? DaysOfWeek, // e.g. [1, 3, 5] for Mon, Wed, Fri
    DateTime? EndDate,
    int? Occurrences,
    TimeSpan StartTime,
    string? Notes,
    string? ClientEmail,
    string? ClientName,
    string? ClientPhone,
    int GroupSize = 1
);

public record BulkCancelRequest(List<Guid> BookingIds, string? Reason);
