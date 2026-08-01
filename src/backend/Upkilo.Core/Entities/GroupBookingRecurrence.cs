using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

public class GroupBookingRecurrence : TenantEntity
{
    [Required]
    public Guid ClassId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Frequency { get; set; } = "weekly";

    public string[] DaysOfWeek { get; set; } = Array.Empty<string>();

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [MaxLength(10)]
    public string StartTime { get; set; } = "09:00:00";

    public int DurationMinutes { get; set; } = 60;

    public int MaxParticipants { get; set; } = 10;

    [ForeignKey("ClassId")]
    public GroupBooking? MasterClass { get; set; }
}
