using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class AvailabilitySnapshot : TenantEntity
{
    public DateTime Date { get; set; }

    [Required]
    public string AvailabilityJson { get; set; } = "{}";

    [MaxLength(100)]
    public string Hash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
