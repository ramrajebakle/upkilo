using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Service for managing core booking operations
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Creates a new booking with all necessary side effects (events, notifications, etc.)
    /// </summary>
    Task<Booking> CreateBookingAsync(Guid tenantId, CreateBookingModel model);

    /// <summary>
    /// Updates an existing booking status and handles related business logic
    /// </summary>
    Task<Booking> UpdateStatusAsync(Guid tenantId, Guid bookingId, BookingStatus newStatus, string? reason = null, byte[]? rowVersion = null);

    /// <summary>
    /// Checks if a slot is available for booking
    /// </summary>
    Task<bool> IsAvailableAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime startTime, int durationMinutes);

    /// <summary>
    /// Reschedules an existing booking
    /// </summary>
    Task<Booking> RescheduleBookingAsync(Guid tenantId, Guid bookingId, DateTime newStartTime, string? confirmationCode = null, byte[]? rowVersion = null, bool bypassCodeCheck = false);

    /// <summary>
    /// Creates a recurring series of bookings
    /// </summary>
    Task<RecurringBookingResult> CreateRecurringBookingAsync(Guid tenantId, CreateRecurringBookingModel model);
}

public record CreateRecurringBookingModel(
    Guid? ClientId,
    Guid ServiceId,
    Guid StaffId,
    DateTime StartDate,
    string Frequency, // "Daily", "Weekly", "Monthly"
    int Interval,
    List<int>? DaysOfWeek,
    DateTime? EndDate,
    int? Occurrences,
    TimeSpan StartTime,
    string? Notes,
    int GroupSize = 1
);

public record RecurringBookingResult(
    Guid PatternId,
    int SuccessCount,
    int ConflictCount,
    List<DateTime> SuccessfulDates,
    List<DateTime> ConflictedDates
);

public record CreateBookingModel(
    Guid? ClientId,
    Guid ServiceId,
    Guid StaffId,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes,
    int GroupSize = 1,
    bool IsWalkIn = false,
    Guid? RecurringPatternId = null,
    Guid? SlotHoldId = null
);
