using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class PaymentDispute : TenantEntity
{
    [Required]
    [MaxLength(100)]
    public string StripeDisputeId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string StripeChargeId { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    [MaxLength(50)]
    public string Currency { get; set; } = "USD";
    
    [MaxLength(50)]
    public string Status { get; set; } = "NeedsResponse";
    
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
}
