using System;

namespace Upkilo.Core.Entities;

public class RefreshToken : TenantEntity
{
    public Guid UserId { get; set; }

    /// <summary>
    /// The actual secure random token string, usually hashed in DB 
    /// but for this implementation we might store it directly or hashed depending on requirements.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public string? ReasonRevoked { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public User? User { get; set; }
}
