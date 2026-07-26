using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities
{
    [Table("tenant_dashboard_stats")]
    public class TenantDashboardStats : TenantEntity
    {
        [Column("total_clients")]
        public int TotalClients { get; set; } = 0;

        [Column("total_bookings")]
        public int TotalBookings { get; set; } = 0;

        [Column("total_revenue")]
        public decimal TotalRevenue { get; set; } = 0;

        [Column("pending_bookings")]
        public int PendingBookings { get; set; } = 0;

        [Column("completed_bookings")]
        public int CompletedBookings { get; set; } = 0;

        [Column("revenue_this_month")]
        public decimal RevenueThisMonth { get; set; } = 0;

        [Column("bookings_this_month")]
        public int BookingsThisMonth { get; set; } = 0;

        [Column("last_updated_at")]
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("tenant_daily_metrics")]
    public class TenantDailyMetric : TenantEntity
    {
        [Column("date")]
        public DateTime Date { get; set; }

        [Column("revenue")]
        public decimal Revenue { get; set; } = 0;

        [Column("booking_count")]
        public int BookingCount { get; set; } = 0;

        [Column("new_client_count")]
        public int NewClientCount { get; set; } = 0;

        [Column("cancelled_booking_count")]
        public int CancelledBookingCount { get; set; } = 0;
    }
}
