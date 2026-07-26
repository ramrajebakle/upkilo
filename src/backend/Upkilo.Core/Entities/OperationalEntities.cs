using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;



/// <summary>
/// Implements Task 1458: Experiment entity
/// </summary>
[Table("experiments")]
public class Experiment : TenantEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string VariantA { get; set; } = "Control";
    public string VariantB { get; set; } = "Variation";
    public bool IsActive { get; set; } = true;
    public double TrafficSplit { get; set; } = 0.5; // 50/50
}
