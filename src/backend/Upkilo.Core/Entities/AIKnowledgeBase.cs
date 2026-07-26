using System;

namespace Upkilo.Core.Entities;

public class AIKnowledgeBase : TenantEntity
{
    public string Category { get; set; } = string.Empty; // e.g., "FAQ", "Service Details", "Policy"
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public bool IsActive { get; set; } = true;
    public float[]? VectorEmbedding { get; set; } // For future vector search
}
