using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class JobQuota : TenantEntity
{
    [Required]
    [MaxLength(100)]
    public string JobType { get; set; } = string.Empty;

    public int LimitPerMonth { get; set; }

    public int CurrentUsage { get; set; }

    public DateTime ResetDate { get; set; }
}
