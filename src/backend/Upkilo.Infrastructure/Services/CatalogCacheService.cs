using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// SC7: Redis L2 read-through cache for high-frequency catalog entities (services, staff, locations).
/// Tenant-scoped cache keys with tag-based invalidation.
/// On cache miss → DB read → populate Redis (15-min TTL).
/// On mutation → evict all keys for the tenant tag.
/// </summary>
public class CatalogCacheService
{
    private readonly IDistributedCache _cache;
    private readonly AppDbContext _context;
    private readonly ILogger<CatalogCacheService> _logger;

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    public CatalogCacheService(IDistributedCache cache, AppDbContext context, ILogger<CatalogCacheService> logger)
    {
        _cache = cache;
        _context = context;
        _logger = logger;
    }

    public async Task<List<Service>> GetServicesAsync(Guid tenantId)
    {
        var key = CacheKey.Services(tenantId);
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
        {
            _logger.LogDebug("[SC7] Cache hit: {Key}", key);
            return JsonSerializer.Deserialize<List<Service>>(cached) ?? new();
        }

        _logger.LogDebug("[SC7] Cache miss: {Key}", key);
        var services = await _context.Services
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .AsNoTracking()
            .ToListAsync();

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(services), _cacheOptions);
        return services;
    }

    public async Task<List<StaffMember>> GetStaffAsync(Guid tenantId)
    {
        var key = CacheKey.Staff(tenantId);
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<List<StaffMember>>(cached) ?? new();

        var staff = await _context.StaffMembers
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .AsNoTracking()
            .ToListAsync();

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(staff), _cacheOptions);
        return staff;
    }

    public async Task<List<Location>> GetLocationsAsync(Guid tenantId)
    {
        var key = CacheKey.Locations(tenantId);
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<List<Location>>(cached) ?? new();

        var locations = await _context.Locations
            .Where(l => l.TenantId == tenantId && l.IsActive)
            .AsNoTracking()
            .ToListAsync();

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(locations), _cacheOptions);
        return locations;
    }

    /// <summary>
    /// Invalidate all catalog cache for a tenant (call after any service/staff/location mutation).
    /// </summary>
    public async Task InvalidateTenantCatalogAsync(Guid tenantId)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKey.Services(tenantId)),
            _cache.RemoveAsync(CacheKey.Staff(tenantId)),
            _cache.RemoveAsync(CacheKey.Locations(tenantId)));

        _logger.LogInformation("[SC7] Evicted catalog cache for tenant {TenantId}", tenantId);
    }

    private static class CacheKey
    {
        public static string Services(Guid tenantId) => $"catalog:services:{tenantId}";
        public static string Staff(Guid tenantId) => $"catalog:staff:{tenantId}";
        public static string Locations(Guid tenantId) => $"catalog:locations:{tenantId}";
    }
}
