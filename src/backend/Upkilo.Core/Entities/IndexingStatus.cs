using System;

namespace Upkilo.Core.Entities;

public class IndexingStatus : TenantEntity
{
    public string PageUrl { get; set; } = string.Empty;
    public string SearchEngine { get; set; } = string.Empty; // Google, Bing
    public bool IsIndexed { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public decimal AveragePosition { get; set; }
}

public class ConversionAnalysis : TenantEntity
{
    public string PageUrl { get; set; } = string.Empty;
    public int PageViews { get; set; }
    public int UniqueVisitors { get; set; }
    public decimal BounceRate { get; set; }
    public decimal ConversionRate { get; set; }
    public string? DropOffPoints { get; set; } // JSON
    public string? CtaSuggestions { get; set; } // JSON
    public string? HeadlineSuggestions { get; set; } // JSON
    public string? AbVariants { get; set; } // JSON
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string? AnalysisData { get; set; } // JSON
    public bool IsApplied { get; set; }
}
