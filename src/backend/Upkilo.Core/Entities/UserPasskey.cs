using System;
using System.Text.Json.Serialization;

namespace Upkilo.Core.Entities;

public class UserPasskey : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>
    /// The public key credential identifier.
    /// </summary>
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The public key associated with the credential.
    /// </summary>
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    public byte[] UserHandle { get; set; } = Array.Empty<byte>();

    public uint SignatureCounter { get; set; }

    public string CredentialType { get; set; } = string.Empty;

    public string Aaguid { get; set; } = string.Empty;

    public DateTime RegDate { get; set; } = DateTime.UtcNow;

    public string RegOrigin { get; set; } = string.Empty;

    // Navigation property if needed
    // [JsonIgnore]
    // public virtual ApplicationUser User { get; set; } = null!;
}
