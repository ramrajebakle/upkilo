using System;

namespace Upkilo.Core.Entities;

public class GdprConsent : TenantEntity
{
    public Guid ClientId { get; set; }
    
    /// <summary>
    /// Type of consent (e.g., "Marketing", "DataProcessing", "Analytics")
    /// </summary>
    public string ConsentType { get; set; } = string.Empty;
    
    public bool IsGranted { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// IP Address at the time of giving consent
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string of the browser/device
    /// </summary>
    public string? UserAgent { get; set; }
    
    public Client? Client { get; set; }
}
