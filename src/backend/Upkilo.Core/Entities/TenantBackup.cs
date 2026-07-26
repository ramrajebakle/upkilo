namespace Upkilo.Core.Entities;

/// <summary>
/// A tenant data backup/export record. Backs the /settings/backup page.
/// The actual archive generation is handled asynchronously; this row tracks its lifecycle.
/// </summary>
public class TenantBackup : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>full | incremental | manual</summary>
    public string Type { get; set; } = "manual";

    /// <summary>completed | in_progress | failed | scheduled</summary>
    public string Status { get; set; } = "in_progress";

    public long SizeBytes { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public string? DownloadUrl { get; set; }

    /// <summary>Serialized array of entity keys included in the backup (clients, bookings, …).</summary>
    public string IncludedEntitiesJson { get; set; } = "[]";

    public bool Restorable { get; set; } = true;
}
