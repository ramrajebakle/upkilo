using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Stores per-tenant integration connection state and settings.
/// Credentials are stored AES-256-GCM encrypted in EncryptedCredentials.
/// Legacy plaintext fields (AccessToken/RefreshToken/ApiKey) are kept for
/// backward-compat but must not be written for new connections.
/// </summary>
public class TenantIntegration : TenantEntity
{
    public string IntegrationId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string IntegrationType { get; set; } = string.Empty;
    public string? ExternalAccountId { get; set; }
    public bool IsActive { get; set; }
    public bool IsConnected { get; set; }

    // Legacy plaintext — kept for EF migrations only; do NOT write new credentials here
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? ApiKey { get; set; }

    // AES-256-GCM encrypted JSON: {"api_key":"…","account_sid":"…", …}
    public string? EncryptedCredentials { get; set; }

    public string? Settings { get; set; } // Non-sensitive config JSON (e.g. sync direction)
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? VerificationError { get; set; }
}
