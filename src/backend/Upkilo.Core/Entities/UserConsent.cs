namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks user consent for various GDPR/legal categories (e.g., Marketing, Essential, Tracking)
/// </summary>
public class UserConsent : TenantEntity
{
    public Guid UserId { get; set; }

    public string Category { get; set; } = string.Empty; // Marketing, Analytics, Essential, ThirdParty SMS
    public string ConsentType { get => Category; set => Category = value; } // Alias

    public bool HasConsented { get; set; }
    public bool IsGranted { get => HasConsented; set => HasConsented = value; } // Alias

    public DateTime ConsentedAt { get; set; }
    public DateTime GrantedAt { get => ConsentedAt; set => ConsentedAt = value; } // Alias

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? ConsentSource { get; set; } // e.g., "WebForm", "Checkout", "AppRegistration"

    public DateTime? RevokedAt { get; set; }

    // Navigation
    public virtual User? User { get; set; }
}
