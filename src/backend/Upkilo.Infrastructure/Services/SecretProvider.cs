using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implementation of ISecretProvider that handles secret retrieval with fallback logic.
/// </summary>
public class SecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretProvider> _logger;

    public SecretProvider(IConfiguration configuration, ILogger<SecretProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetSecret(string key)
    {
        // 1. Try Environment Variables (prefixed for security/scope if needed)
        var envValue = Environment.GetEnvironmentVariable(key.Replace(":", "__"));
        if (!string.IsNullOrEmpty(envValue)) return envValue;

        // 2. Try regular Configuration (which includes User Secrets and appsettings during development)
        var configValue = _configuration[key];
        if (!string.IsNullOrEmpty(configValue)) return configValue;

        _logger.LogWarning("Secret with key '{Key}' not found in any provider.", key);
        return null;
    }

    public string GetSecret(string key, string defaultValue)
    {
        return GetSecret(key) ?? defaultValue;
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        // For now, sync over async since we're using IConfiguration fallback
        return await Task.FromResult(GetSecret(key));
    }

    public async Task<string> GetSecretAsync(string key, string defaultValue)
    {
        return await Task.FromResult(GetSecret(key) ?? defaultValue);
    }
}
