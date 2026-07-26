using System;

namespace Upkilo.Core.Entities;

public class AdAccount : TenantEntity
{
    public string Platform { get; set; } = string.Empty; // Meta, Google, LinkedIn
    public string ExternalAccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool IsConnected { get; set; }
    public string Status { get; set; } = "Active"; // Active, Disabled, Pending
    public string? ErrorMessage { get; set; }
}
