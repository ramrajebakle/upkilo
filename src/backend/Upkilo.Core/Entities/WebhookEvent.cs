using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class WebhookEvent : TenantEntity
{
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string Payload { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string EndpointUrl { get; set; } = string.Empty;
    
    public int RetryCount { get; set; }
    
    public string Status { get; set; } = "Pending";
    
    public DateTime? NextRetryAt { get; set; }
}
