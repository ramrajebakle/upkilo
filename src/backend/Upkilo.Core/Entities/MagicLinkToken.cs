using System;
using System.ComponentModel.DataAnnotations;

namespace Upkilo.Core.Entities;

public class MagicLinkToken : TenantEntity
{
    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    // Navigation
    public Client? Client { get; set; }
}
