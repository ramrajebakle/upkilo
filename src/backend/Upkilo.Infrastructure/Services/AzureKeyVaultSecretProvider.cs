using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Production-grade secret provider using Azure Key Vault with local configuration fallback.
/// </summary>
public class AzureKeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient? _secretClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureKeyVaultSecretProvider> _logger;
    private readonly bool _isProduction;

    public AzureKeyVaultSecretProvider(
        IConfiguration configuration,
        ILogger<AzureKeyVaultSecretProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Support both config key conventions:
        // - "AzureKeyVault:VaultUri"  (set by appsettings.json and Bicep env var AzureKeyVault__VaultUri)
        // - "Azure:KeyVault:Uri"      (legacy / alternative convention)
        var vaultUri = _configuration["AzureKeyVault:VaultUri"]
                    ?? _configuration["Azure:KeyVault:Uri"];
        _isProduction = _configuration["ASPNETCORE_ENVIRONMENT"] == "Production"
                     || _configuration["Environment"] == "Production";

        if (!string.IsNullOrEmpty(vaultUri))
        {
            try
            {
                // In production, uses Managed Identity. In Dev, uses Azure CLI/Visual Studio/Env credentials.
                _secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                _logger.LogInformation("Azure Key Vault client initialized for {VaultUri}", vaultUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault client. Falling back to configuration.");
            }
        }
    }

    public string? GetSecret(string key)
    {
        // 1. Try Key Vault if available (Sync over Async for this interface implementation)
        if (_secretClient != null)
        {
            try
            {
                var secret = _secretClient.GetSecret(key.Replace(":", "--")).Value;
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Secret {Key} not found in Key Vault: {Message}", key, ex.Message);
            }
        }

        // 2. Fallback to Environment Variables
        var envValue = Environment.GetEnvironmentVariable(key.Replace(":", "__"));
        if (!string.IsNullOrEmpty(envValue)) return envValue;

        // 3. Fallback to appsettings/UserSecrets
        return _configuration[key];
    }

    public string GetSecret(string key, string defaultValue)
    {
        return GetSecret(key) ?? defaultValue;
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        if (_secretClient != null)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync(key.Replace(":", "--"));
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogTrace("Secret {Key} not found in Key Vault: {Message}", key, ex.Message);
            }
        }

        return GetSecret(key);
    }

    public async Task<string> GetSecretAsync(string key, string defaultValue)
    {
        return await GetSecretAsync(key) ?? defaultValue;
    }
}
