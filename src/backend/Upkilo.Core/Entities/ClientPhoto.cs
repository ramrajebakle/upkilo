namespace Upkilo.Core.Entities;

/// <summary>
/// Client photo entity - stores client profile pics, before/after photos, documents
/// </summary>
public class ClientPhoto : TenantEntity
{
    public Guid ClientId { get; set; }
    public PhotoType Type { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public DateTime? TakenAt { get; set; }
    public Guid? ServiceId { get; set; } // Associated service
    public Guid? BookingId { get; set; } // Associated booking
    public bool IsPublic { get; set; } // Client portfolio permission
    public int? Width { get; set; }
    public int? Height { get; set; }

    // Navigation
    public virtual Client? Client { get; set; }
    public virtual Service? Service { get; set; }
    public virtual Booking? Booking { get; set; }
}

public enum PhotoType
{
    Profile,
    Before,
    After,
    Document,
    IDVerification,
    Other
}
