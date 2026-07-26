using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public class LandingPage : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // /promo/spring-sale
    public string? Description { get; set; }
    public string HtmlContent { get; set; } = string.Empty;
    public string? CssOverrides { get; set; }
    public bool IsPublished { get; set; }
    public Guid? CampaignId { get; set; }
    public string? VariantGroup { get; set; } // A/B test group ID
    public string? VariantLabel { get; set; } // "A" or "B"
    public int Views { get; set; }
    public int Conversions { get; set; }
    public DateTime? PublishedAt { get; set; }

    // Navigation
    public virtual Campaign? Campaign { get; set; }
    public virtual ICollection<LeadCapture> Leads { get; set; } = new List<LeadCapture>();
}
