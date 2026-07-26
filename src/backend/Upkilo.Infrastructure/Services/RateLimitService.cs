using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
}

public class RedisRateLimitService : IRateLimitService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisRateLimitService> _logger;

    public RedisRateLimitService(IDistributedCache cache, ILogger<RedisRateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
    {
        var cacheKey = $"rl:{key}";
        try
        {
            var currentCountStr = await _cache.GetStringAsync(cacheKey);
            var currentCount = 0;

            if (currentCountStr != null)
            {
                currentCount = int.Parse(currentCountStr);
            }

            if (currentCount >= limit)
            {
                return false;
            }

            // Increment and set expiry if new
            currentCount++;
            await _cache.SetStringAsync(cacheKey, currentCount.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key {Key}", key);
            return true; // Fail open for resilience
        }
    }
}
