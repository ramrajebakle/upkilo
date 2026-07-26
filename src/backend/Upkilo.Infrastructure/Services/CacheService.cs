using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using StackExchange.Redis;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Tenant-aware caching service with cache stampede prevention.
/// All keys are automatically scoped to the tenant to prevent cross-tenant data leaks.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IBusinessMetrics _metrics;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public CacheService(IDistributedCache cache, IBusinessMetrics metrics, ILogger<CacheService> logger, IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
        _redis = redis;
    }

    private string TenantKey(Guid tenantId, string key) => $"t:{tenantId}:{key}";

    private TimeSpan GetJitteredExpiration(TimeSpan baseExpiration)
    {
        // Add +/- 15% jitter to prevent cache stampedes
        var jitterPercentage = (Random.Shared.NextDouble() * 0.3) - 0.15;
        var jitteredTicks = (long)(baseExpiration.Ticks * (1 + jitterPercentage));
        return TimeSpan.FromTicks(jitteredTicks);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        return await GetOrSetAsync<T>(Guid.Empty, key, factory, expiry);
    }

    public async Task<T?> GetOrSetAsync<T>(Guid tenantId, string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        var cacheKey = TenantKey(tenantId, key);
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                _metrics.RecordCacheHit(key); // Simplified name for metric
                return JsonSerializer.Deserialize<T>(cached);
            }
            _metrics.RecordCacheMiss(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for {Key}, falling back to factory", cacheKey);
        }

        // Cache miss — fetch from source
        var result = await factory();

        try
        {
            var baseExpiry = expiry ?? DefaultExpiry;
            // Do NOT set SlidingExpiration alongside AbsoluteExpiration — Redis evicts at
            // whichever comes first. A 2-min sliding window kills high-traffic entries before
            // the intended 10-min absolute expiry, causing unnecessary cache churn.
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = GetJitteredExpiration(baseExpiry)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for {Key}", cacheKey);
        }

        return result;
    }

    public async Task RemoveAsync(string key)
    {
        await InvalidateAsync(Guid.Empty, key);
    }

    public async Task InvalidateAsync(Guid tenantId, string key)
    {
        try
        {
            await _cache.RemoveAsync(TenantKey(tenantId, key));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache invalidation failed for {Key}", key);
        }
    }

    public async Task InvalidatePatternAsync(Guid tenantId, string prefix)
    {
        var pattern = TenantKey(tenantId, prefix) + "*";
        _logger.LogInformation("Pattern invalidation requested for {Pattern}", pattern);

        if (_redis != null)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    var keys = server.Keys(pattern: pattern).ToArray();
                    if (keys.Any())
                    {
                        var db = _redis.GetDatabase();
                        await db.KeyDeleteAsync(keys);
                        _logger.LogInformation("Invalidated {Count} keys for pattern {Pattern}", keys.Length, pattern);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache pattern {Pattern} via Redis", pattern);
            }
        }
        else
        {
            _logger.LogWarning("IConnectionMultiplexer is not registered. Cannot perform pattern invalidation for {Pattern}.", pattern);
        }
    }
}
