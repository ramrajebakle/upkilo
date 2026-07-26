using System;

namespace Upkilo.Core.Entities;

public class SeoAnalysis : TenantEntity
{
    public string PageUrl { get; set; } = string.Empty;
    public string? CurrentTitle { get; set; }
    public string? SuggestedTitle { get; set; }
    public string? CurrentMetaDescription { get; set; }
    public string? SuggestedMetaDescription { get; set; }
    public string? StructuredDataJson { get; set; } // JSON-LD
    public string? InternalLinkSuggestions { get; set; } // JSON
    public string? ContentGaps { get; set; } // JSON
    public int? Score { get; set; } // 0-100
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
