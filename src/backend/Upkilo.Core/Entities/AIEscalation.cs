using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks high-risk or uncertain autonomous decisions requiring human review.
/// </summary>
public class AIEscalation : TenantEntity
{
    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty; // AI, Workflow, Security

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "High"; // Low, Medium, High, Critical

    public string? MetadataJson { get; set; } // Detailed context (JSON)

    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }

    public bool RequiresApproval { get; set; }
    public bool IsApproved { get; set; }
    public string? ActionTaken { get; set; } // Approved, Rejected, Overridden

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
