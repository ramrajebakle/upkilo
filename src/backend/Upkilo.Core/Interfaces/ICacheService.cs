namespace Upkilo.Core.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task<T?> GetOrSetAsync<T>(Guid tenantId, string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task InvalidateAsync(Guid tenantId, string key);
    Task InvalidatePatternAsync(Guid tenantId, string prefix);
}
