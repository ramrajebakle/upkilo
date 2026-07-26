namespace Upkilo.Core.Entities;

/// <summary>
/// Client contraindication entity - tracks medical conditions, allergies, and safety notes
/// </summary>
public class ClientContraindication : TenantEntity
{
    public Guid ClientId { get; set; }
    public ContraindicationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ContraindicationSeverity Severity { get; set; } = ContraindicationSeverity.Moderate;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; } // For temporary conditions
    public Guid? AddedByUserId { get; set; }

    // Navigation
    public virtual Client? Client { get; set; }
}

public enum ContraindicationType
{
    Allergy,
    MedicalCondition,
    Medication,
    Injury,
    Pregnancy,
    SkinCondition,
    Other
}

public enum ContraindicationSeverity
{
    Low,      // Informational only
    Moderate, // Caution required
    High,     // Service may need modification
    Critical  // Service should be declined
}
