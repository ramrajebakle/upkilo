using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class DunningCycle : TenantEntity
{
    [Required]
    [MaxLength(100)]
    public string StripeInvoiceId { get; set; } = string.Empty;
    
    public int AttemptCount { get; set; }
    
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Active";
    
    public bool SubscriptionSuspended { get; set; }
}
