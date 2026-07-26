namespace Upkilo.Core.ReadModels;

/// <summary>
/// Lightweight projection of a Booking for calendar rendering.
/// </summary>
public record BookingCalendarItem(
    Guid Id,
    string ClientName,
    string ServiceName,
    string? StaffName,
    string Color,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    decimal Price,
    bool IsWalkIn
);

/// <summary>
/// Aggregated metrics for the business dashboard.
/// </summary>
public record DashboardAggregates(
    decimal TotalRevenue,
    int TotalBookings,
    int NewClients,
    int CompletedBookings,
    double OccupancyRate,
    decimal RevenueChange,
    int BookingsChange,
    int ClientsChange,
    List<RevenueByDay> RevenueTrend,
    List<TopService> TopServices,
    List<StaffSummary> StaffSummaries
);

/// <summary>
/// A single data-point in the revenue trend series.
/// </summary>
public record RevenueByDay(string Date, decimal Revenue, int Bookings);

/// <summary>
/// Aggregated metrics for a single service.
/// </summary>
public record TopService(Guid Id, string Name, int Bookings, decimal Revenue);

/// <summary>
/// Aggregated performance metrics for a single staff member.
/// </summary>
public record StaffSummary(Guid Id, string Name, int Bookings, decimal Revenue, double UtilizationRate);

/// <summary>
/// Flat report of bookings within a date range, including aggregate counts.
/// </summary>
public record BookingReport(
    int TotalBookings,
    int Completed,
    int Cancelled,
    int NoShows,
    decimal TotalRevenue,
    double CancellationRate,
    double NoShowRate,
    List<BookingCalendarItem> Items
);
