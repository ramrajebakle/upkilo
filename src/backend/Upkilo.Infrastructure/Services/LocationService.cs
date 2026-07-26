using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class LocationService : ILocationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LocationService> _logger;

    public LocationService(AppDbContext context, ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Location> CreateAsync(Guid tenantId, Location location)
    {
        location.Id = Guid.NewGuid();
        location.TenantId = tenantId;
        location.CreatedAt = DateTime.UtcNow;
        location.UpdatedAt = DateTime.UtcNow;

        // If this is the first location, make it primary
        var hasLocations = _context.Set<Location>().Any(l => l.TenantId == tenantId);
        if (!hasLocations)
            location.IsPrimary = true;

        _context.Set<Location>().Add(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Location {Name} created for tenant {TenantId}", location.Name, tenantId);
        return location;
    }

    public async Task<IEnumerable<Location>> GetAllAsync(Guid tenantId)
    {
        return await Task.FromResult(
            _context.Set<Location>()
                .Where(l => l.TenantId == tenantId)
                .OrderBy(l => l.SortOrder)
                .ThenBy(l => l.Name)
                .ToList()
        );
    }

    public async Task<Location?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await Task.FromResult(
            _context.Set<Location>()
                .FirstOrDefault(l => l.Id == id && l.TenantId == tenantId)
        );
    }

    public async Task<Location?> UpdateAsync(Guid id, Guid tenantId, Location updates)
    {
        var location = await GetByIdAsync(id, tenantId);
        if (location == null) return null;

        location.Name = updates.Name ?? location.Name;
        location.Description = updates.Description ?? location.Description;
        location.AddressLine1 = updates.AddressLine1 ?? location.AddressLine1;
        location.AddressLine2 = updates.AddressLine2 ?? location.AddressLine2;
        location.City = updates.City ?? location.City;
        location.State = updates.State ?? location.State;
        location.Country = updates.Country ?? location.Country;
        location.PostalCode = updates.PostalCode ?? location.PostalCode;
        location.Phone = updates.Phone ?? location.Phone;
        location.Email = updates.Email ?? location.Email;
        location.Timezone = updates.Timezone ?? location.Timezone;
        location.IsActive = updates.IsActive;
        location.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return location;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId)
    {
        var location = await GetByIdAsync(id, tenantId);
        if (location == null) return false;

        _context.Set<Location>().Remove(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Location {Id} deleted for tenant {TenantId}", id, tenantId);
        return true;
    }

    public async Task<bool> SetDefaultAsync(Guid id, Guid tenantId)
    {
        var locations = _context.Set<Location>().Where(l => l.TenantId == tenantId).ToList();
        
        foreach (var loc in locations)
        {
            loc.IsPrimary = loc.Id == id;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
