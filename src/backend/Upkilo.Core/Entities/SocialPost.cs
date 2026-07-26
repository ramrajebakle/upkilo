using System;

namespace Upkilo.Core.Entities;

public class SocialPost : TenantEntity
{
    public string Platform { get; set; } = string.Empty; // LinkedIn, Twitter, Instagram, Facebook
    public string Content { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public string? Hashtags { get; set; } // JSON array
    public string? CTA { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaUrlsJson { get; set; }
    public string Tone { get; set; } = "Professional"; // Professional, Casual, Inspirational
    public string Status { get; set; } = "Draft"; // Draft, Scheduled, Posted, Failed
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? ExternalPostId { get; set; }
    public int Impressions { get; set; }
    public int Engagements { get; set; }
}
