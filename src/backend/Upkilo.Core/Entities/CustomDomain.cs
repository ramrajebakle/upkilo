using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class CustomDomain : BaseEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Hostname { get; set; } = string.Empty; // e.g., booking.acme.com

    public bool IsVerified { get; set; }

    [MaxLength(100)]
    public string VerificationToken { get; set; } = string.Empty;

    public DomainSslStatus SslStatus { get; set; } = DomainSslStatus.Pending;

    public DateTime? LastVerifiedAt { get; set; }

    // Navigation
    public virtual Tenant? Tenant { get; set; }
}

public enum DomainSslStatus
{
    Pending,
    Active,
    Failed,
    Expired
}
