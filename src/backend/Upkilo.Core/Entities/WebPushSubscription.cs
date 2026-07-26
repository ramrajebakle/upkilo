namespace Upkilo.Core.Entities;

/// <summary>
/// Web push subscription details for browser-based push notifications (VAPID)
/// </summary>
public class WebPushSubscription : TenantEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// The canonical endpoint that a push message is sent to.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The public key used for message encryption (P256DH).
    /// </summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// The authentication secret used for message encryption (Auth).
    /// </summary>
    public string Auth { get; set; } = string.Empty;

    /// <summary>
    /// The browser/device name or identifier.
    /// </summary>
    public string? Tag { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
