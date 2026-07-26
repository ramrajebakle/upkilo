using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Sales pipeline definition for CRM deal tracking.
/// </summary>
public class SalesPipeline : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PipelineStage> Stages { get; set; } = new List<PipelineStage>();
    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();
}

public enum DealStatus
{
    Open,
    Won,
    Lost,
    OnHold
}

public class PipelineStage : TenantEntity
{
    public Guid PipelineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } // Compatibility alias
    public int OrderIndex { get => DisplayOrder; set => DisplayOrder = value; } // Alias
    public decimal WinProbability { get; set; } // 0-100%
    public decimal ProbabilityPercentage { get => WinProbability; set => WinProbability = value; } // Alias
    public string Color { get; set; } = "#3B82F6";

    public virtual SalesPipeline? Pipeline { get; set; }
}

public class Deal : TenantEntity
{
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public Guid? ClientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public DealStatus Status { get; set; } = DealStatus.Open;
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? ActualCloseDate { get; set; } // Compatibility alias
    public string? LossReason { get; set; }
    public string? LostReason { get; set; } // Compatibility alias
    public Guid? AssignedToId { get; set; }
    public Guid? AssignedToStaffId { get; set; } // Compatibility alias

    public virtual SalesPipeline? Pipeline { get; set; }
    public virtual PipelineStage? Stage { get; set; }
    public virtual Client? Client { get; set; }
    public virtual ICollection<DealActivity> Activities { get; set; } = new List<DealActivity>();
}

public class DealActivity : TenantEntity
{
    public Guid DealId { get; set; }
    public string ActivityType { get; set; } = string.Empty; // Note, Call, Email, Meeting, StageChanged
    public string Description { get; set; } = string.Empty;
    public Guid? PerformedById { get; set; }
    public string? Metadata { get; set; } // JSON

    public virtual Deal? Deal { get; set; }
}
