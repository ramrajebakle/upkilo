using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediatR;
using Upkilo.Core.Events;
using Upkilo.Infrastructure.Background;

namespace Upkilo.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly ISchedulingService _schedulingService;
    private readonly IEventService _eventService;
    private readonly IMediator _mediator;

    public BookingService(
        AppDbContext context,
        ILogger<BookingService> logger,
        ISchedulingService schedulingService,
        IEventService eventService,
        IMediator mediator)
    {
        _context = context;
        _logger = logger;
        _schedulingService = schedulingService;
        _eventService = eventService;
        _mediator = mediator;
    }

    public async Task<Booking> CreateBookingAsync(Guid tenantId, CreateBookingModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == model.ServiceId && s.TenantId == tenantId);
            if (service == null) throw new InvalidOperationException("Service not found");

            // 1. Concurrency Check (Tenant Level)
            if (!await _schedulingService.CheckConcurrencyLimitAsync(tenantId))
            {
                throw new InvalidOperationException("Concurrency limit reached for this tenant.");
            }

            // L-8 FIX: Ensure time is always treated as UTC internally to prevent offset bugs
            if (model.StartTime.Kind != DateTimeKind.Utc)
            {
                model = model with { 
                    StartTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Utc),
                    EndTime = DateTime.SpecifyKind(model.EndTime, DateTimeKind.Utc)
                };
            }

            // 2. Slot Verification with Pessimistic Locking
            if (model.SlotHoldId.HasValue)
            {
                // Lock the hold record to prevent concurrent conversion
                var hold = await _context.SlotHolds
                    .FromSqlRaw("SELECT * FROM \"SlotHolds\" WHERE \"Id\" = {0} AND \"TenantId\" = {1} FOR UPDATE", model.SlotHoldId.Value, tenantId)
                    .FirstOrDefaultAsync();

                if (hold == null || hold.IsReleased || hold.ExpiresAt < DateTime.UtcNow)
                {
                    throw new InvalidOperationException("The time slot hold has expired or is invalid. Please try reserving the slot again.");
                }

                // Verify hold matches requested slot
                if (hold.StaffId != model.StaffId || hold.SlotDateTime != model.StartTime || hold.ServiceId != model.ServiceId)
                {
                    throw new InvalidOperationException("The time slot hold does not match the requested booking details.");
                }

                // Consume Hold
                hold.IsReleased = true;
                hold.IsConverted = true;
            }
            else
            {
                // Fallback for direct bookings (Admin/Walk-in)
                if (model.StaffId != Guid.Empty)
                {
                    try
                    {
                        if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "SELECT 1 FROM \"StaffMembers\" WHERE \"Id\" = {0} AND \"TenantId\" = {1} FOR UPDATE NOWAIT",
                                model.StaffId, tenantId);
                        }
                    }
                    catch (Exception ex) when (ex.ToString().Contains("55P03") || ex.ToString().Contains("could not obtain lock"))
                    {
                        throw new InvalidOperationException("This time slot is currently being booked by someone else. Please try again or select another slot.");
                    }
                }
                
                if (!await _schedulingService.IsSlotAvailableAsync(tenantId, model.ServiceId, model.StaffId, model.StartTime, service.DurationMinutes))
                {
                    throw new InvalidOperationException("The requested time slot is no longer available.");
                }
            }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = model.ClientId,
            ServiceId = model.ServiceId,
            StaffId = model.StaffId,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Status = BookingStatus.Confirmed,
            Price = service.Price,
            Notes = model.Notes,
            GroupSize = model.GroupSize,
            IsWalkIn = model.IsWalkIn,
            RecurringPatternId = model.RecurringPatternId,
            CreatedAt = DateTime.UtcNow
        };

        // H-07 FIX: Generate cryptographically secure confirmation code
        booking.Metadata["ConfirmationCode"] = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));

        if (model.IsWalkIn)
        {
            booking.CheckedInAt = DateTime.UtcNow;
        }

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // 3. Update Availability Cache
        await _schedulingService.UpdateAvailabilityCacheAsync(tenantId, booking.StaffId ?? Guid.Empty, DateOnly.FromDateTime(booking.StartTime));

        _logger.LogInformation("Booking created via service: {BookingId}", booking.Id);

        // Publish Events
        await _eventService.PublishAsync(model.IsWalkIn ? "booking.walkin" : "booking.created", booking, tenantId);

        var domainEvent = new BookingCreated
        {
            BookingId = booking.Id,
            ClientId = booking.ClientId ?? Guid.Empty,
            ServiceId = booking.ServiceId ?? Guid.Empty,
            StaffId = booking.StaffId ?? Guid.Empty,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Price = booking.Price ?? 0m,
            IsWalkIn = model.IsWalkIn,
            TenantId = tenantId
        };
        await _mediator.Publish(new BookingCreatedNotification(domainEvent));

        return booking;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Booking> UpdateStatusAsync(Guid tenantId, Guid bookingId, BookingStatus newStatus, string? reason = null, byte[]? rowVersion = null)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);
        if (booking == null) throw new InvalidOperationException("Booking not found");

        // H-9 FIX: Strictly require RowVersion to prevent "last write wins" concurrency bugs.
        // If the caller didn't supply it, we abort.
        if (rowVersion == null || rowVersion.Length == 0)
        {
            throw new InvalidOperationException("Concurrency token (RowVersion) is required to update a booking.");
        }
        
        _context.Entry(booking).Property(b => b.RowVersion).OriginalValue = rowVersion;

        var oldStatus = booking.Status;
        if (oldStatus == newStatus) return booking;

        // M-8 FIX: Prevent invalid status transitions. Once cancelled or completed, 
        // the booking cannot be reverted or changed.
        if (oldStatus == BookingStatus.Cancelled || oldStatus == BookingStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot change status from {oldStatus}. Booking is in a terminal state.");
        }

        booking.Status = newStatus;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.Version++; // Explicit version tracking
        if (reason != null) booking.Notes = (booking.Notes ?? "") + "\nStatus change: " + reason;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict detected for booking {BookingId}", bookingId);
            throw new InvalidOperationException("This booking was modified by another user. Please refresh and try again.");
        }

        if (newStatus == BookingStatus.Completed)
        {
            await _eventService.PublishAsync("booking.completed", booking, tenantId);
            await _mediator.Publish(new BookingCompletedNotification(new BookingCompleted
            {
                BookingId = booking.Id,
                ClientId = booking.ClientId ?? Guid.Empty,
                StaffId = booking.StaffId ?? Guid.Empty,
                FinalPrice = booking.Price ?? 0m,
                TenantId = tenantId
            }));
        }
        else if (newStatus == BookingStatus.Cancelled)
        {
            await _eventService.PublishAsync("booking.cancelled", booking, tenantId);
            await _mediator.Publish(new BookingCancelledNotification(new BookingCancelled
            {
                BookingId = booking.Id,
                ClientId = booking.ClientId ?? Guid.Empty,
                CancellationReason = reason ?? "Cancelled via system",
                ByClient = false,
                TenantId = tenantId
            }));

            // Invalidate Cache for real-time updates
            await _schedulingService.InvalidateStaffCacheAsync(tenantId, booking.StaffId ?? Guid.Empty, DateOnly.FromDateTime(booking.StartTime));
        }

        return booking;
    }

    public async Task<Booking> RescheduleBookingAsync(Guid tenantId, Guid bookingId, DateTime newStartTime, string? confirmationCode = null, byte[]? rowVersion = null, bool bypassCodeCheck = false)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);

        if (booking == null) throw new InvalidOperationException("Booking not found");

        // H-9 FIX: Strictly require RowVersion to prevent "last write wins"
        if (rowVersion == null || rowVersion.Length == 0)
        {
            throw new InvalidOperationException("Concurrency token (RowVersion) is required to reschedule a booking.");
        }
        
        _context.Entry(booking).Property(b => b.RowVersion).OriginalValue = rowVersion;

        // L-8 FIX: Ensure new time is treated as UTC
        if (newStartTime.Kind != DateTimeKind.Utc)
        {
            newStartTime = DateTime.SpecifyKind(newStartTime, DateTimeKind.Utc);
        }

        // Security check for public rescheduling
        if (!bypassCodeCheck)
        {
            string? expectedCode = null;
            if (booking.Metadata != null && booking.Metadata.TryGetValue("ConfirmationCode", out var codeObj))
            {
                expectedCode = codeObj?.ToString()?.ToUpper();
            }

            // Fallback for old bookings that don't have a secure code
            if (string.IsNullOrEmpty(expectedCode))
            {
                expectedCode = booking.Id.ToString().Substring(0, 8).ToUpper();
            }

            if (string.IsNullOrEmpty(confirmationCode) || confirmationCode.ToUpper() != expectedCode)
            {
                throw new UnauthorizedAccessException("Valid confirmation code is required to reschedule this booking.");
            }
        }

        // Business Logic Checks
        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            throw new InvalidOperationException("Cannot reschedule a cancelled or completed booking.");

        int maxReschedules = 2;
        if (booking.Tenant?.Settings != null && booking.Tenant.Settings.TryGetValue("booking_max_reschedules", out var maxObj) && int.TryParse(maxObj.ToString(), out var m))
            maxReschedules = m;

        if (booking.RescheduleCount >= maxReschedules)
            throw new InvalidOperationException($"Maximum number of reschedules ({maxReschedules}) reached.");

        int noticeHours = 24;
        if (booking.Tenant?.Settings != null && booking.Tenant.Settings.TryGetValue("booking_notice_period_hours", out var hoursObj) && int.TryParse(hoursObj.ToString(), out var h))
            noticeHours = h;

        if (booking.StartTime <= DateTime.UtcNow.AddHours(noticeHours))
            throw new InvalidOperationException($"Cannot reschedule within {noticeHours} hours of the appointment.");

        // Availability Check
        var service = booking.Service;
        if (service == null) throw new InvalidOperationException("Service not found for this booking.");

        bool isAvailable = await _schedulingService.IsSlotAvailableAsync(tenantId, booking.ServiceId ?? Guid.Empty, booking.StaffId, newStartTime, service.DurationMinutes);
        if (!isAvailable) throw new InvalidOperationException("The selected time slot is no longer available.");

        // Update Booking
        var oldStartTime = booking.StartTime;
        var oldEndTime = booking.EndTime;
        
        booking.StartTime = newStartTime;
        booking.EndTime = newStartTime.AddMinutes(service.DurationMinutes);
        booking.RescheduleCount++;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.Version++; // Explicit version tracking

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict detected while rescheduling booking {BookingId}", bookingId);
            throw new InvalidOperationException("The booking or schedule has been updated by another user. Please refresh and try again.");
        }

        // Update Availability Cache
        await _schedulingService.UpdateAvailabilityCacheAsync(tenantId, booking.StaffId ?? Guid.Empty, DateOnly.FromDateTime(oldStartTime));
        await _schedulingService.UpdateAvailabilityCacheAsync(tenantId, booking.StaffId ?? Guid.Empty, DateOnly.FromDateTime(newStartTime));

        // Publish Events
        await _eventService.PublishAsync("booking.rescheduled", new {
            BookingId = booking.Id,
            OldStartTime = oldStartTime,
            NewStartTime = booking.StartTime,
            RescheduleCount = booking.RescheduleCount
        }, tenantId);

        var domainEvent = new BookingRescheduled
        {
            BookingId = booking.Id,
            OldStartTime = oldStartTime,
            NewStartTime = booking.StartTime,
            OldEndTime = oldEndTime,
            NewEndTime = booking.EndTime,
            TenantId = tenantId
        };
        await _mediator.Publish(new BookingRescheduledNotification(domainEvent));

        return booking;
    }


    public async Task<RecurringBookingResult> CreateRecurringBookingAsync(Guid tenantId, CreateRecurringBookingModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == model.ServiceId && s.TenantId == tenantId);
            if (service == null) throw new InvalidOperationException("Service not found");

            // 1. Generate target dates
            var candidateDates = await _schedulingService.GenerateRecurrenceDatesAsync(
                tenantId,
                model.Frequency,
                model.Interval,
                model.StartDate,
                model.EndDate,
                model.Occurrences,
                model.DaysOfWeek);

            if (candidateDates.Count == 0)
            {
                throw new InvalidOperationException("No valid dates could be generated with the given pattern.");
            }

            // 2. Validate Availability per slot
            var successfulDates = new List<DateTime>();
            var conflictedDates = new List<DateTime>();

            foreach (var date in candidateDates)
            {
                var targetDateTime = date.Add(model.StartTime);
                bool isAvailable = await _schedulingService.IsSlotAvailableAsync(
                    tenantId,
                    model.ServiceId,
                    model.StaffId,
                    targetDateTime,
                    service.DurationMinutes);

                if (isAvailable)
                {
                    successfulDates.Add(targetDateTime);
                }
                else
                {
                    conflictedDates.Add(targetDateTime);
                }
            }

            if (successfulDates.Count == 0)
            {
                return new RecurringBookingResult(Guid.Empty, 0, conflictedDates.Count, new List<DateTime>(), conflictedDates);
            }

            // 3. Create Pattern and Bookings
            var pattern = new RecurringPattern
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Frequency = model.Frequency,
                Interval = model.Interval,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Occurrences = successfulDates.Count,
                DaysOfWeek = model.DaysOfWeek != null ? System.Text.Json.JsonSerializer.Serialize(model.DaysOfWeek) : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecurringPatterns.Add(pattern);

            var bookings = new List<Booking>();
            foreach (var date in successfulDates)
            {
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ClientId = model.ClientId,
                    ServiceId = model.ServiceId,
                    StaffId = model.StaffId,
                    StartTime = date,
                    EndTime = date.AddMinutes(service.DurationMinutes),
                    Status = BookingStatus.Confirmed,
                    Price = service.Price,
                    Notes = model.Notes,
                    GroupSize = model.GroupSize,
                    RecurringPatternId = pattern.Id,
                    CreatedAt = DateTime.UtcNow
                };
                
                bookings.Add(booking);
                _context.Bookings.Add(booking);

                // Publish individual events
                await _eventService.PublishAsync("booking.created", booking, tenantId);

                var domainEvent = new BookingCreated
                {
                    BookingId = booking.Id,
                    ClientId = booking.ClientId ?? Guid.Empty,
                    ServiceId = booking.ServiceId ?? Guid.Empty,
                    StaffId = booking.StaffId ?? Guid.Empty,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Price = booking.Price ?? 0m,
                    IsWalkIn = false,
                    TenantId = tenantId
                };
                await _mediator.Publish(new BookingCreatedNotification(domainEvent));
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Created recurring pattern {PatternId} with {Count} bookings", pattern.Id, bookings.Count);

            // Invalidate Caches for all affected dates
            var uniqueDates = successfulDates.Select(d => DateOnly.FromDateTime(d)).Distinct();
            foreach (var date in uniqueDates)
            {
                await _schedulingService.UpdateAvailabilityCacheAsync(tenantId, model.StaffId, date);
            }

            // Fire event for the series
            await _eventService.PublishAsync("booking.recurring_created", new {
                PatternId = pattern.Id,
                BookingIds = bookings.Select(b => b.Id).ToList()
            }, tenantId);

            return new RecurringBookingResult(
                pattern.Id,
                successfulDates.Count,
                conflictedDates.Count,
                successfulDates,
                conflictedDates);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating recurring booking series");
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime startTime, int durationMinutes)
    {
        return await _schedulingService.IsSlotAvailableAsync(tenantId, serviceId, staffId, startTime, durationMinutes);
    }
}
