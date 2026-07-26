using Upkilo.Core.ReadModels;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// CQRS read-side service for booking projections.
/// Provides calendar, dashboard aggregate, and report views
/// scoped to a single tenant and date range.
/// </summary>
public interface IBookingReadModelService
{
    /// <summary>
    /// Returns bookings within [from, to) suitable for calendar rendering.
    /// Optionally filtered by staff and/or service.
    /// </summary>
    Task<IReadOnlyList<BookingCalendarItem>> GetCalendarAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        Guid? staffId = null,
        Guid? serviceId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns aggregated KPIs and trend data for the dashboard.
    /// Includes period-over-period change metrics.
    /// </summary>
    Task<DashboardAggregates> GetDashboardAggregatesAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a denormalized booking report with aggregate counts and a flat item list.
    /// Optionally filtered by location.
    /// </summary>
    Task<BookingReport> GetBookingReportAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        Guid? locationId = null,
        CancellationToken ct = default);
}
