namespace Upkilo.Core.Entities;

/// <summary>
/// Group booking: allows multiple participants for a single time slot.
/// Used for couples massages, group classes, family appointments, etc.
/// Tracks the master booking and all participant bookings.
/// </summary>
public class GroupBooking : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid MasterBookingId { get; set; }     // The initial booking that created the group
    public Guid OrganizerId { get; set; }          // Client who organized/booked the group
    public string? GroupName { get; set; }          // e.g., "Smith Family Session"
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public GroupBookingStatus Status { get; set; } = GroupBookingStatus.Open;
    public decimal TotalPrice { get; set; }
    public bool IsPublic { get; set; }              // Can others find and join?
    public string? Notes { get; set; }

    // Navigation
    public ICollection<GroupBookingParticipant> Participants { get; set; } = new List<GroupBookingParticipant>();
}

/// <summary>
/// Individual participant in a group booking.
/// </summary>
public class GroupBookingParticipant : BaseEntity
{
    public Guid GroupBookingId { get; set; }
    public Guid? ClientId { get; set; }             // Null if guest hasn't registered
    public string? GuestName { get; set; }          // For unregistered guests
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Confirmed;
    public decimal? IndividualPrice { get; set; }

    // Navigation
    public GroupBooking? GroupBooking { get; set; }
    public Client? Client { get; set; }
}

public enum GroupBookingStatus
{
    Open,           // Accepting more participants
    Full,           // Max capacity reached
    Confirmed,      // All participants confirmed, locked
    Completed,      // Appointment done
    Cancelled       // Group booking cancelled
}

public enum ParticipantStatus
{
    Invited,        // Invitation sent
    Confirmed,      // Accepted
    Declined,       // Rejected
    NoShow,         // Didn't attend
    Attended        // Was present
}
