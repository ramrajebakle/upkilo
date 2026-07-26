using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Service for dynamic reporting and data aggregation across the platform.
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Executes a dynamic report based on a ReportDefinition's ConfigJson and filters.
    /// </summary>
    Task<ReportDataResult> ExecuteReportAsync(Guid tenantId, ReportDefinition definition, Dictionary<string, string>? runtimeFilters = null);

    /// <summary>
    /// Generates a funnel conversion dataset for a given period.
    /// </summary>
    Task<FunnelResult> GetFunnelAnalyticsAsync(Guid tenantId, DateTime from, DateTime to);

    /// <summary>
    /// Dispatches scheduled reports via email.
    /// </summary>
    Task ProcessScheduledReportsAsync();
}

public class ReportDataResult
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public Dictionary<string, object> Summary { get; set; } = new();
}

public class FunnelResult
{
    public List<FunnelStepData> Steps { get; set; } = new();
    public decimal OverallConversionRate { get; set; }
}

public class FunnelStepData
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal DropOffRate { get; set; }
}
