using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Caches webhook delivery nonces to prevent replay attacks
/// </summary>
public class WebhookNonceCache
{
    private readonly ConcurrentDictionary<string, DateTime> _nonces = new();
    private readonly ILogger<WebhookNonceCache> _logger;
    private readonly TimeSpan _nonceExpiry = TimeSpan.FromMinutes(5);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public WebhookNonceCache(ILogger<WebhookNonceCache> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns true if the nonce has NOT been seen before (valid new request)
    /// Returns false if the nonce HAS been seen (replay attack)
    /// </summary>
    public bool ValidateNonce(string nonce)
    {
        CleanupExpired();

        if (_nonces.TryAdd(nonce, DateTime.UtcNow))
        {
            return true; // New nonce, valid
        }

        _logger.LogWarning("Webhook replay detected: nonce {Nonce} already used", nonce);
        return false; // Duplicate nonce, replay
    }

    /// <summary>
    /// Generates a unique nonce for outbound webhook signatures
    /// </summary>
    public string GenerateNonce()
    {
        var nonce = $"{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        _nonces.TryAdd(nonce, DateTime.UtcNow);
        return nonce;
    }

    private void CleanupExpired()
    {
        if (DateTime.UtcNow - _lastCleanup < TimeSpan.FromMinutes(1)) return;

        var cutoff = DateTime.UtcNow - _nonceExpiry;
        var expired = _nonces.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
        {
            _nonces.TryRemove(key, out _);
        }
        _lastCleanup = DateTime.UtcNow;

        if (expired.Count > 0)
            _logger.LogDebug("Cleaned up {Count} expired webhook nonces", expired.Count);
    }
}
