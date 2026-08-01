using System;

namespace Upkilo.Core.Entities;

public class StaffCertification : TenantEntity
{
    public Guid StaffId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public string? IssuingAuthority { get => IssuingOrganization; set => IssuingOrganization = value; } // Alias

    public string? CertificateNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? VerificationUrl { get; set; }
    public string? DocumentUrl { get => VerificationUrl; set => VerificationUrl = value; } // Alias
    public string Status { get; set; } = "Active"; // Active, Expired, Revoked

    public virtual StaffMember? Staff { get; set; }
}
