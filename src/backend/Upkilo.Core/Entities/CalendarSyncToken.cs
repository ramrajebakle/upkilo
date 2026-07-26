using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Upkilo.Core.Entities;

/// <summary>
/// Stores OAuth tokens and sync state for external calendar integration
/// </summary>
[Table("calendar_sync_tokens")]
public class CalendarSyncToken : TenantEntity
{
    [Required]
    public Guid StaffId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = "google"; // "google" or "outlook"

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// For incremental sync (e.g., Google's nextSyncToken or Outlook's deltaLink)
    /// </summary>
    public string? SyncToken { get; set; }

    public bool IsActive { get; set; } = true;
    public string? ExternalAccountId { get; set; }
    public string SyncDirection { get; set; } = "both"; // "push", "pull", "both"

    [ForeignKey(nameof(StaffId))]
    public virtual StaffMember? Staff { get; set; }
}
