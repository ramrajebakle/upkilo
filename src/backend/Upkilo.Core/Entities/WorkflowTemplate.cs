using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

/// <summary>
/// Pre-defined workflow template
/// </summary>
[Table("workflow_templates")]
public class WorkflowTemplate : TenantEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string TriggerType { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string TriggerConfig { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string Steps { get; set; } = "[]";

    [MaxLength(50)]
    public string Category { get; set; } = "custom";

    public bool IsPublic { get; set; }
}
