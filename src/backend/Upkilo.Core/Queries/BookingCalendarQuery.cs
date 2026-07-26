using Upkilo.Core.Interfaces.CQRS;

namespace Upkilo.Core.Queries;

/// <summary>
/// CQRS Query — denormalized booking calendar view.
/// Returns all bookings for a date range in a flat, render-ready structure.
/// </summary>
public class BookingCalendarQuery : IQuery<BookingCalendarReadModel>
{
    public Guid TenantId { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string? StaffId { get; init; }
    public string? ServiceId { get; init; }
    public string? Status { get; init; } // filter: confirmed, pending, etc.
}

public class BookingCalendarReadModel
{
    public List<CalendarBookingItem> Bookings { get; init; } = new();
    public int Total { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public class CalendarBookingItem
{
    public string Id { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string ClientInitials { get; init; } = string.Empty;
    public string? ClientEmail { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? ServiceColor { get; init; }
    public string StaffName { get; init; } = string.Empty;
    public string? StaffColor { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int DurationMinutes { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? Notes { get; init; }
}
