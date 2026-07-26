namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks tenant onboarding progress.
/// Each tenant has one progress record that tracks which setup steps
/// they've completed, skipped, or haven't started yet.
/// </summary>
public class TenantOnboardingProgress : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    // Step completion tracking
    public bool BusinessProfileCompleted { get; set; }
    public bool WorkingHoursCompleted { get; set; }
    public bool ServicesAdded { get; set; }
    public bool StaffAdded { get; set; }
    public bool BookingPageCustomized { get; set; }
    public bool PaymentSetupCompleted { get; set; }
    public bool FirstBookingCreated { get; set; }
    public bool ClientsImported { get; set; }

    // Timestamps
    public DateTime? BusinessProfileCompletedAt { get; set; }
    public DateTime? WorkingHoursCompletedAt { get; set; }
    public DateTime? ServicesAddedAt { get; set; }
    public DateTime? StaffAddedAt { get; set; }
    public DateTime? BookingPageCustomizedAt { get; set; }
    public DateTime? PaymentSetupCompletedAt { get; set; }
    public DateTime? FirstBookingCreatedAt { get; set; }
    public DateTime? ClientsImportedAt { get; set; }

    public bool IsDismissed { get; set; }
    public DateTime? DismissedAt { get; set; }
    public string? SampleDataTemplate { get; set; } // which template was seeded
    public DateTime? DripEmailSentAt { get; set; }  // tracks 7-day drip email to avoid re-sending

    // Navigation
    public Tenant? Tenant { get; set; }
}
