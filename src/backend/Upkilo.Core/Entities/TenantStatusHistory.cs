using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class TenantStatusHistory : TenantEntity
{
    [Required]
    [MaxLength(50)]
    public string PreviousStatus { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NewStatus { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public Guid? ChangedByUserId { get; set; }
}
