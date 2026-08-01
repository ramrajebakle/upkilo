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
    // `new`: this is a domain version STRING (e.g. "1.2.0"), deliberately distinct from
    // BaseEntity.Version, which is an int concurrency counter. Declaring it `new` documents
    // the shadowing and silences CS0108. NOTE: this entity therefore has no usable
    // BaseEntity.Version concurrency token — see docs/PRODUCTION_DEPLOYMENT.md §4.
    public new string Version { get; set; } = "1.0";

    public Guid? AcceptedByUserId { get; set; }

    public DateTime? AcceptedAt { get; set; }

    [MaxLength(100)]
    public string IpAddress { get; set; } = string.Empty;
}
