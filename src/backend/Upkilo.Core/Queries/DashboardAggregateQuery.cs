using Upkilo.Core.Interfaces.CQRS;

namespace Upkilo.Core.Queries;

/// <summary>
/// CQRS Query — pre-aggregated dashboard KPIs for fast rendering.
/// Denormalized projection rebuilt on schedule; never hits OLTP tables directly.
/// </summary>
public class DashboardAggregateQuery : IQuery<DashboardAggregateReadModel>
{
    public Guid TenantId { get; init; }
    public string Period { get; init; } = "30d"; // 7d, 30d, 90d, ytd
}

public class DashboardAggregateReadModel
{
    // Revenue KPIs
    public decimal TotalRevenue { get; init; }
    public decimal RevenueChange { get; init; }       // % vs prior period
    public decimal PendingRevenue { get; init; }

    // Booking KPIs
    public int TotalBookings { get; init; }
    public int BookingsChange { get; init; }           // absolute vs prior period
    public int CompletedBookings { get; init; }
    public int CancelledBookings { get; init; }
    public int NoShowBookings { get; init; }
    public double CancellationRate { get; init; }

    // Client KPIs
    public int TotalClients { get; init; }
    public int NewClients { get; init; }
    public int ReturningClients { get; init; }
    public double RetentionRate { get; init; }

    // Staff KPIs
    public int ActiveStaff { get; init; }
    public double AvgUtilizationRate { get; init; }

    // Time series (for charts)
    public List<DailyRevenueSample> RevenueByDay { get; init; } = new();
    public List<DailyBookingSample> BookingsByDay { get; init; } = new();

    // Top performers
    public List<TopServiceItem> TopServices { get; init; } = new();
    public List<TopStaffItem> TopStaff { get; init; } = new();

    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public string Period { get; init; } = string.Empty;
}

public record DailyRevenueSample(string Date, decimal Revenue, int Bookings);
public record DailyBookingSample(string Date, int Confirmed, int Cancelled, int NoShow);
public record TopServiceItem(string Id, string Name, int BookingCount, decimal Revenue);
public record TopStaffItem(string Id, string Name, int BookingCount, double UtilizationPct);
