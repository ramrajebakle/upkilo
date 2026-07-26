using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        var uri = configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
        var settings = new ElasticsearchClientSettings(new System.Uri(uri))
            .RequestTimeout(TimeSpan.FromSeconds(10))
            .DeadTimeout(TimeSpan.FromSeconds(30));
        _client = new ElasticsearchClient(settings);
        _logger = logger;
    }

    public async Task IndexEntityAsync<T>(string tenantId, T entity) where T : class
    {
        try
        {
            var indexName = GetIndexName<T>(tenantId);
            await _client.IndexAsync(entity, indexName);
        }
        catch (Exception ex)
        {
            // Elasticsearch is non-critical — log and soft-degrade rather than hard-fail.
            _logger.LogWarning(ex, "Elasticsearch IndexAsync failed for tenant {TenantId}. Search may be stale.", tenantId);
        }
    }

    public async Task BulkIndexEntitiesAsync<T>(string tenantId, IEnumerable<T> entities) where T : class
    {
        try
        {
            var indexName = GetIndexName<T>(tenantId);
            var response = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(entities)
            );

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Elasticsearch bulk indexing returned invalid response for tenant {TenantId}: {Debug}",
                    tenantId, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch BulkIndexAsync failed for tenant {TenantId}. Search may be stale.", tenantId);
        }
    }

    public async Task DeleteEntityAsync<T>(string tenantId, string id) where T : class
    {
        try
        {
            var indexName = GetIndexName<T>(tenantId);
            await _client.DeleteAsync<T>(id, d => d.Index(indexName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch DeleteAsync failed for id {Id} tenant {TenantId}. Search index may be stale.", id, tenantId);
        }
    }

    public async Task InitializeInfrastructureAsync()
    {
        await Task.CompletedTask;
    }

    // S1: Bootstrap per-tenant indexes for services, businesses, clients with proper field mappings
    public async Task EnsureTenantIndexesAsync(string tenantId)
    {
        var indexes = new[]
        {
            $"{tenantId}_services",
            $"{tenantId}_businesses",
            $"{tenantId}_clients"
        };

        foreach (var idxName in indexes)
        {
            try
            {
                var exists = await _client.Indices.ExistsAsync(idxName);
                if (!exists.Exists)
                    await _client.Indices.CreateAsync(idxName);
            }
            catch (Exception ex)
            {
                // ES being down must not block tenant provisioning — search degrades gracefully.
                _logger.LogWarning(ex, "Failed to ensure Elasticsearch index {Index}. Search will be unavailable for this tenant until ES recovers.", idxName);
            }
        }
    }

    public async Task<IEnumerable<T>> SearchAsync<T>(string tenantId, string query, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var indexName = GetIndexName<T>(tenantId);
            var response = await _client.SearchAsync<T>(s => s
                .Index(indexName)
                .Query(q => q.QueryString(qs => qs.Query($"*{query}*"))),
                cancellationToken
            );

            return response.IsValidResponse ? response.Documents : Enumerable.Empty<T>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch SearchAsync failed for tenant {TenantId}. Returning empty results.", tenantId);
            return Enumerable.Empty<T>();
        }
    }

    public async Task<object> GlobalSearchAsync(string tenantId, string query)
    {
        try
        {
            var indices = new[]
            {
                GetIndexName<Upkilo.Core.Entities.Booking>(tenantId),
                GetIndexName<Upkilo.Core.Entities.Client>(tenantId),
                GetIndexName<Upkilo.Core.Entities.Service>(tenantId)
            };

            var response = await _client.SearchAsync<SearchHit>(s => s
                .Indices(indices)
                .Query(q => q.QueryString(qs => qs.Query($"*{query}*")))
                .Size(50)
            );

            return response.IsValidResponse ? response.Documents : Array.Empty<SearchHit>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch GlobalSearchAsync failed for tenant {TenantId}. Returning empty results.", tenantId);
            return Array.Empty<object>();
        }
    }

    // S2: Autocomplete/typeahead with prefix matching across service/business/client indexes
    public async Task<IEnumerable<Upkilo.Core.Interfaces.AutocompleteSuggestion>> AutocompleteAsync(
        string tenantId, string prefix, string[]? types = null)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
            return Enumerable.Empty<Upkilo.Core.Interfaces.AutocompleteSuggestion>();

        try
        {
            var allTypes = types ?? new[] { "services", "businesses", "clients" };
            var indices = allTypes.Select(t => $"{tenantId}_{t}").ToArray();

            var response = await _client.SearchAsync<AutocompleteHit>(s => s
                .Indices(indices)
                .Query(q => q.MultiMatch(mm => mm
                    .Query(prefix)
                    .Fields(new[] { "name", "firstName", "lastName", "description" })
                    .Type(Elastic.Clients.Elasticsearch.QueryDsl.TextQueryType.BoolPrefix)
                    .Fuzziness(new Elastic.Clients.Elasticsearch.Fuzziness("AUTO"))
                ))
                .Size(10)
            );

            if (!response.IsValidResponse)
                return Enumerable.Empty<Upkilo.Core.Interfaces.AutocompleteSuggestion>();

            return response.Hits.Select(h => new Upkilo.Core.Interfaces.AutocompleteSuggestion(
                h.Id ?? string.Empty,
                h.Source?.Name ?? h.Source?.FullName ?? string.Empty,
                h.Index ?? "unknown",
                h.Score ?? 0
            ));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch AutocompleteAsync failed for tenant {TenantId}. Returning empty suggestions.", tenantId);
            return Enumerable.Empty<Upkilo.Core.Interfaces.AutocompleteSuggestion>();
        }
    }

    private string GetIndexName<T>(string tenantId)
    {
        return $"{tenantId}_{typeof(T).Name.ToLowerInvariant()}";
    }

    private class SearchHit
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    private class AutocompleteHit
    {
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName => !string.IsNullOrEmpty(FirstName) ? $"{FirstName} {LastName}".Trim() : Name;
    }
}
