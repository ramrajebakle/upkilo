using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IElasticsearchService
{
    Task InitializeInfrastructureAsync();
    // S1: Bootstrap per-tenant indexes with proper mappings
    Task EnsureTenantIndexesAsync(string tenantId);
    Task IndexEntityAsync<T>(string tenantId, T entity) where T : class;
    Task BulkIndexEntitiesAsync<T>(string tenantId, IEnumerable<T> entities) where T : class;
    Task DeleteEntityAsync<T>(string tenantId, string id) where T : class;
    Task<IEnumerable<T>> SearchAsync<T>(string tenantId, string query, CancellationToken cancellationToken = default) where T : class;
    Task<object> GlobalSearchAsync(string tenantId, string query);
    // S2: Autocomplete/typeahead with fuzzy matching
    Task<IEnumerable<AutocompleteSuggestion>> AutocompleteAsync(string tenantId, string prefix, string[]? types = null);
}

public record AutocompleteSuggestion(string Id, string Text, string Type, double Score);
