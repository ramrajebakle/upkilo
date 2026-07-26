using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class TwoFaRecoveryRequest : BaseEntity
{
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string IdentityVerificationData { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    [MaxLength(500)]
    public string AdminNotes { get; set; } = string.Empty;
    
    public Guid? ResolvedByAdminId { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
}
