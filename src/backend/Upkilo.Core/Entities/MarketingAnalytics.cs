using System;

namespace Upkilo.Core.Entities;

public class MarketingAnalytics : TenantEntity
{
    public string MetricType { get; set; } = string.Empty; // TrafficGrowth, LeadVolume, ConversionRate, Revenue
    public string Source { get; set; } = string.Empty; // Organic, Social, Direct, Paid
    public decimal Value { get; set; }
    public DateTime RecordDate { get; set; }
    public string? Insight { get; set; } // Plain-English AI insight
    public bool IsAnomaly { get; set; }
}

public class MarketingForecast : TenantEntity
{
    public string ForecastType { get; set; } = string.Empty; // Traffic, Leads, Revenue
    public int HorizonDays { get; set; } // 14, 30, 90
    public decimal PredictedValue { get; set; }
    public decimal ConfidencePercent { get; set; }
    public DateTime ForecastDate { get; set; }
    public string? Methodology { get; set; } // JSON
}

public class AgentAction : TenantEntity
{
    public string AgentName { get; set; } = string.Empty; // SEO, Content, Discovery, Distribution, LeadOptimization, Analytics
    public string ActionType { get; set; } = string.Empty; // Generated, Updated, Published, Optimized
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical
    public bool RequiresReview { get; set; }
    public bool WasAutoApplied { get; set; }
    public bool WasRolledBack { get; set; }
    public string? Metadata { get; set; } // JSON
}
