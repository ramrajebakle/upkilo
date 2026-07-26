using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implements Task 1346: Notification rate caps per tenant
/// Implements Task 1437: SMS/WhatsApp rate limits
/// </summary>
public class ThrottlingService
{
    private readonly ILogger<ThrottlingService> _logger;
    private static readonly ConcurrentDictionary<string, int> _tenantUsage = new();
    private static readonly ConcurrentDictionary<string, DateTime> _expiryTimes = new();

    public ThrottlingService(ILogger<ThrottlingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsThrottledAsync(Guid tenantId, string actionType, int limitPerHour)
    {
        var key = $"{tenantId}:{actionType}";
        var now = DateTime.UtcNow;

        // Reset if hour passed
        if (_expiryTimes.TryGetValue(key, out var expiry) && now > expiry)
        {
            _tenantUsage.TryRemove(key, out _);
            _expiryTimes.TryRemove(key, out _);
        }

        var count = _tenantUsage.AddOrUpdate(key, 1, (k, v) => v + 1);
        if (!_expiryTimes.ContainsKey(key))
        {
            _expiryTimes.TryAdd(key, now.AddHours(1));
        }

        if (count > limitPerHour)
        {
            _logger.LogWarning("Tenant {TenantId} throttled for {ActionType}. Count: {Count}", tenantId, actionType, count);
            return true;
        }

        await Task.CompletedTask;
        return false;
    }
}
