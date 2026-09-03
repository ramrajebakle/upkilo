using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IElasticsearchService
{
    /// <summary>
    /// False when Elasticsearch:Uri is not configured. Callers use this to tell "the search ran
    /// and matched nothing" apart from "search is not running at all" — the two are otherwise
    /// indistinguishable, because every failure path here returns an empty result set.
    /// </summary>
    bool IsAvailable { get; }

    Task InitializeInfrastructureAsync();
    // S1: Bootstrap per-tenant indexes with proper mappings
    Task EnsureTenantIndexesAsync(string tenantId);
    Task IndexEntityAsync<T>(string tenantId, T entity) where T : class;
    Task BulkIndexEntitiesAsync<T>(string tenantId, IEnumerable<T> entities) where T : class;
    Task DeleteEntityAsync<T>(string tenantId, string id) where T : class;

    /// <summary>
    /// Delete by runtime type, for callers that only have one — notably the EF interceptor,
    /// whose compile-time type is `object`.
    /// </summary>
    Task DeleteEntityAsync(string tenantId, Type entityType, string id);
    Task<IEnumerable<T>> SearchAsync<T>(string tenantId, string query, CancellationToken cancellationToken = default) where T : class;
    Task<object> GlobalSearchAsync(string tenantId, string query);
    // S2: Autocomplete/typeahead with fuzzy matching
    Task<IEnumerable<AutocompleteSuggestion>> AutocompleteAsync(string tenantId, string prefix, string[]? types = null);
}

public record AutocompleteSuggestion(string Id, string Text, string Type, double Score);
