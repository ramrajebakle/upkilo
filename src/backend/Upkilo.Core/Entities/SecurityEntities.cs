namespace Upkilo.Core.Entities;

/// <summary>
/// Tracks failed login attempts for brute force protection
/// </summary>
public class LoginAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public bool Succeeded { get; set; }
}

/// <summary>
/// Tracks email verification tokens
/// </summary>
public class EmailVerificationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// Tracks password history to prevent reuse
/// </summary>
public class PasswordHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User? User { get; set; }
}
