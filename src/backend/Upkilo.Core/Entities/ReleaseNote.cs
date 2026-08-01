namespace Upkilo.Core.Entities;

public class ReleaseNote : BaseEntity
{
    // `new`: this is a domain version STRING (e.g. "1.2.0"), deliberately distinct from
    // BaseEntity.Version, which is an int concurrency counter. Declaring it `new` documents
    // the shadowing and silences CS0108. NOTE: this entity therefore has no usable
    // BaseEntity.Version concurrency token — see docs/PRODUCTION_DEPLOYMENT.md §4.
    public new string Version { get; set; } = string.Empty; // e.g., "1.2.0"
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsDraft { get; set; }

    public string TargetAudience { get; set; } = "All"; // All, Admins, Staff, Clients
    public string FeaturesTagsJson { get; set; } = "[]"; // Serialized array of feature tags
}
