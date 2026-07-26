using System;

namespace Upkilo.Core.Entities;

public class GeneratedContent : TenantEntity
{
    public string ContentType { get; set; } = string.Empty; // BlogPost, LandingSection, FAQ, SocialPost
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Keywords { get; set; } // JSON array
    public string? IntentCluster { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Scheduled, Published, Rejected
    public DateTime? ScheduledAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsAIGenerated { get; set; } = true;
    public string? DuplicateCheckHash { get; set; } // For duplicate content prevention
    public int? WordCount { get; set; }
}
