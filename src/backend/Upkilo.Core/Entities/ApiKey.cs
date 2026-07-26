using Upkilo.Core.Entities;

namespace Upkilo.Core.Entities;

/// <summary>
/// API Key for external integrations
/// </summary>
public class ApiKey : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty; // e.g., "upk_live_"
    public string KeyHash { get; set; } = string.Empty; // Store hashed key
    public string LastFourChars { get; set; } = string.Empty;
    
    /// <summary>
    /// List of scopes/permissions (e.g., "read:bookings", "write:clients")
    /// </summary>
    public List<string> Scopes { get; set; } = new();
    
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? GracePeriodExpiresAt { get; set; } // Supporting safe rotation
    public bool IsActive { get; set; } = true;
}
