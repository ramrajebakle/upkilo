namespace Upkilo.Core.Entities;

/// <summary>
/// Digital waiver/consent form for appointments.
/// Clients sign waivers before their appointment (liability, consent, HIPAA, etc.).
/// Waiver text is stored as HTML for rich formatting.
/// </summary>
public class DigitalWaiver : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;          // e.g., "Massage Therapy Consent"
    public string Content { get; set; } = string.Empty;        // HTML content of the waiver
    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int? ExpiryDays { get; set; }                       // Re-sign after N days (null = never)
    public string? ApplicableServiceIds { get; set; }           // Comma-separated (null = all)
    public int Version { get; set; } = 1;

    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<WaiverSignature> Signatures { get; set; } = new List<WaiverSignature>();
}

/// <summary>
/// Client's signature on a digital waiver.
/// Captures signature data, IP, and timestamp for legal compliance.
/// </summary>
public class WaiverSignature : BaseEntity
{
    public Guid WaiverId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? BookingId { get; set; }
    public string SignatureData { get; set; } = string.Empty;   // Base64 SVG or drawn signature
    public string? SignedFromIP { get; set; }
    public string? UserAgent { get; set; }
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public int WaiverVersion { get; set; }                      // Which version was signed

    // Navigation
    public DigitalWaiver? Waiver { get; set; }
    public Client? Client { get; set; }
}
