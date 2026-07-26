using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class UserTourProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TourKey { get; set; } = string.Empty; // e.g., "dashboard-tour", "booking-setup-tour"
    public int CurrentStep { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastInteractedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
