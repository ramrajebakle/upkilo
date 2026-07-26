using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

public class CustomReportDefinition : TenantEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, MaxLength(50)]
    public string ReportType { get; set; } = "Table"; // Table, Chart, Metric

    public string? JsonConfiguration { get; set; } // The JSON defining dimensions, metrics, filters

    public bool IsScheduled { get; set; }
    public string? ScheduleCron { get; set; }
    public string? ScheduledEmailRecipients { get; set; } // Comma separated emails

    public DateTime? LastRunAt { get; set; }
}
