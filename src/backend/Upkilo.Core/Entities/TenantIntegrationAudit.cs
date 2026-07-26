namespace Upkilo.Core.Entities;

/// <summary>
/// Immutable audit trail for every connect/disconnect/update action on tenant integrations.
/// Intentionally NOT a TenantEntity (no soft-delete, no global filter) so records survive
/// tenant deletion and can be queried by platform admins.
/// TenantId is stored explicitly for manual filtering in admin queries.
/// </summary>
public class TenantIntegrationAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string IntegrationId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "connected", "disconnected", "updated", "verified", "verify_failed"
    public string? ActorUserId { get; set; }
    public string? ActorIp { get; set; }
    public string? Details { get; set; } // JSON – safe metadata only, never credentials
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
