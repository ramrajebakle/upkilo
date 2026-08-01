using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

public class RecurringPattern : TenantEntity
{
    public string Frequency { get; set; } = "Weekly"; // Daily, Weekly, Monthly
    public int Interval { get; set; } = 1; // Every X weeks
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Occurrences { get; set; }

    /// <summary>
    /// Days of week for weekly recurrence (e.g., [1, 3, 5] for Mon, Wed, Fri)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DaysOfWeek { get; set; } // JSON array of ints

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
