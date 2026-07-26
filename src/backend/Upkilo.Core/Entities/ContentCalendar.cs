using System;

namespace Upkilo.Core.Entities;

public class ContentCalendar : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // Blog, Social, FAQ
    public string Platform { get; set; } = string.Empty; // Website, LinkedIn, Twitter, Instagram, Facebook
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Published, Skipped
    public Guid? GeneratedContentId { get; set; }
    public virtual GeneratedContent? GeneratedContent { get; set; }
}
