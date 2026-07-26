using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class LegalAgreement : TenantEntity
{
    [Required]
    [MaxLength(200)]
    public string AgreementType { get; set; } = string.Empty;
    
    [Required]
    public string DocumentUrl { get; set; } = string.Empty;
    
    [Required]
    public string Version { get; set; } = "1.0";
    
    public Guid? AcceptedByUserId { get; set; }
    
    public DateTime? AcceptedAt { get; set; }
    
    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;
}
