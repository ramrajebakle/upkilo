namespace Upkilo.Core.Entities;

public class ReleaseNote : BaseEntity
{
    public string Version { get; set; } = string.Empty; // e.g., "1.2.0"
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsDraft { get; set; }
    
    public string TargetAudience { get; set; } = "All"; // All, Admins, Staff, Clients
    public string FeaturesTagsJson { get; set; } = "[]"; // Serialized array of feature tags
}
