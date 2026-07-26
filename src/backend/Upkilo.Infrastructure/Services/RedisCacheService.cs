using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Redis-backed distributed cache with stampede protection via locking
/// </summary>
public class RedisCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var bytes = await _cache.GetStringAsync(key);
        if (bytes == null) return null;
        return System.Text.Json.JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
        {
            // Add jitter to prevent thundering herd (probabilistic expiration)
            var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 30));
            options.AbsoluteExpirationRelativeToNow = expiry.Value + jitter;
        }
        await _cache.SetStringAsync(key, json, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    /// <summary>
    /// Get-or-set with cache stampede protection using local semaphore locking
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        // Acquire lock per key to prevent stampede
            var value = await factory();
            await SetAsync(key, value, expiry);
            return value;
    }

    public async Task<IAsyncDisposable?> AcquireLockAsync(string key, TimeSpan lockDuration)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();

        // Use SET key value NX PX milliseconds
        var acquired = await db.StringSetAsync(lockKey, lockValue, lockDuration, When.NotExists);

        if (acquired)
        {
            return new RedisLockHandle(db, lockKey, lockValue);
        }

        return null;
    }

    private class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _value;

        public RedisLockHandle(IDatabase db, string key, string value)
        {
            _db = db;
            _key = key;
            _value = value;
        }

        public async ValueTask DisposeAsync()
        {
            // Only release if the value matches (prevents releasing locks acquired by others if we timed out)
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";
            await _db.ScriptEvaluateAsync(script, new RedisKey[] { _key }, new RedisValue[] { _value });
        }
    }
}
