using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class DataExport : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid RequestedById { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Processing";

    [MaxLength(500)]
    public string DownloadUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FileUrl { get; set; } // Generated file URL

    [MaxLength(100)]
    public string? TargetEntity { get; set; } // Entity type being exported (Clients, Bookings, etc.)

    public string? FiltersJson { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? Fields { get; set; }

    [MaxLength(50)]
    public string Format { get; set; } = "csv";

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
