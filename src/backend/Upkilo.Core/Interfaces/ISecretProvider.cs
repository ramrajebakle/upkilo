namespace Upkilo.Core.Interfaces;

/// <summary>
/// Interface for securely retrieving secrets from various providers (Key Vault, Environment, Config).
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Gets a secret value by its key.
    /// </summary>
    string? GetSecret(string key);

    /// <summary>
    /// Gets a secret value or a default if not found.
    /// </summary>
    string GetSecret(string key, string defaultValue);

    /// <summary>
    /// Gets a secret value asynchronously.
    /// </summary>
    Task<string?> GetSecretAsync(string key);

    /// <summary>
    /// Gets a secret value asynchronously with default.
    /// </summary>
    Task<string> GetSecretAsync(string key, string defaultValue);
}
