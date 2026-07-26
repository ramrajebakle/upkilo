namespace Upkilo.Core.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<T>> SearchAsync<T>(string query, Guid tenantId) where T : class;
}
