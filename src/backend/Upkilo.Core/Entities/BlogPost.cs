namespace Upkilo.Core.Entities;

public class BlogPost : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Published, Archived
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; } = 0;
    public string? Author { get; set; }
}
