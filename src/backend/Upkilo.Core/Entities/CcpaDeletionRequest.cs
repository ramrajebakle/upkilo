namespace Upkilo.Core.Entities;

/// <summary>
/// Persistent CCPA §1798.105 deletion request record.
/// Replaces the in-memory ConcurrentDictionary that was lost on restart.
/// </summary>
public class CcpaDeletionRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueBy { get; set; }
    /// <summary>pending | fulfilled | rejected</summary>
    public string Status { get; set; } = "pending";
    public DateTime? FulfilledAt { get; set; }
}
