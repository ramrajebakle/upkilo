using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ILocationService
{
    Task<Location> CreateAsync(Guid tenantId, Location location);
    Task<IEnumerable<Location>> GetAllAsync(Guid tenantId);
    Task<Location?> GetByIdAsync(Guid id, Guid tenantId);
    Task<Location?> UpdateAsync(Guid id, Guid tenantId, Location updates);
    Task<bool> DeleteAsync(Guid id, Guid tenantId);
    Task<bool> SetDefaultAsync(Guid id, Guid tenantId);
}
