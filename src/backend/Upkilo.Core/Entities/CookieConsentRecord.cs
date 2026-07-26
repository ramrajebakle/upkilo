namespace Upkilo.Core.Entities;

/// <summary>
/// Persistent server-side record of each cookie consent action.
/// Required for GDPR Art. 7(1) — controller must demonstrate consent was given.
/// </summary>
public class CookieConsentRecord : BaseEntity
{
    public Guid? UserId { get; set; }
    public bool Essential { get; set; } = true;
    public bool Analytics { get; set; }
    public bool Marketing { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string ConsentVersion { get; set; } = "1.0";
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
}
