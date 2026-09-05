using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient? _client;
    private readonly ILogger<ElasticsearchService> _logger;

    /// <summary>
    /// The ONE place an index name is built. Everything - writes, reads, autocomplete and index
    /// creation - resolves through this map.
    ///
    /// It did not exist, and the three call sites disagreed, so the feature could not work at
    /// all:
    ///   - writes went to "{tenant}_object", because SearchSyncInterceptor calls
    ///     IndexEntityAsync(tenantId, entry.Entity) where entry.Entity is statically `object`,
    ///     so typeof(T).Name was "Object";
    ///   - GlobalSearchAsync read "{tenant}_booking" / "_client" / "_service" (singular);
    ///   - AutocompleteAsync and EnsureTenantIndexesAsync used "_services" / "_businesses" /
    ///     "_clients" (plural).
    ///
    /// Writes and reads therefore never touched the same index, and the two read paths did not
    /// agree with each other either. Provisioning Elasticsearch would have produced a search
    /// that returned nothing while looking correctly configured.
    ///
    /// Plural wins because EnsureTenantIndexesAsync already creates the plural names, so
    /// existing indexes stay valid. "businesses" is deliberately absent: no entity is ever
    /// indexed into it.
    /// </summary>
    private static readonly IReadOnlyDictionary<Type, string> IndexSuffixByType =
        new Dictionary<Type, string>
        {
            [typeof(Client)] = "clients",
            [typeof(Booking)] = "bookings",
            [typeof(Service)] = "services",
        };

    /// <summary>Suffixes a caller may name via ?type=. Anything else is ignored.</summary>
    private static readonly HashSet<string> KnownSuffixes =
        IndexSuffixByType.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Fields worth matching on. Named explicitly so a query cannot reach a field the caller
    /// was never meant to search - see the query-injection note on BuildTextQuery.
    /// </summary>
    private static readonly string[] SearchableFields =
        { "name", "firstName", "lastName", "email", "description" };

    public bool IsAvailable { get; }

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;

        // Only treat Elasticsearch as available when a URI was actually configured.
        //
        // The previous default of "http://localhost:9200" meant an unprovisioned deployment -
        // which is every deployment today - still built a client and issued a real request on
        // every search, then waited out the 10s RequestTimeout before returning empty. That is
        // a request thread parked for ten seconds per search, on a B1 instance, for a feature
        // that cannot succeed. Now it short-circuits.
        var uri = configuration["Elasticsearch:Uri"];
        IsAvailable = !string.IsNullOrWhiteSpace(uri);

        if (!IsAvailable)
        {
            _logger.LogInformation(
                "Elasticsearch:Uri is not configured - search returns no results and no requests "
                + "are issued. Set Elasticsearch__Uri to enable it.");
            return;
        }

        var settings = new ElasticsearchClientSettings(new System.Uri(uri!))
            .RequestTimeout(TimeSpan.FromSeconds(10))
            .DeadTimeout(TimeSpan.FromSeconds(30));
        _client = new ElasticsearchClient(settings);
    }

    public async Task IndexEntityAsync<T>(string tenantId, T entity) where T : class
    {
        if (_client == null || entity == null) return;

        // entity.GetType(), NOT typeof(T): the interceptor's compile-time T is `object`.
        if (!TryGetIndexName(entity.GetType(), tenantId, out var indexName)) return;

        try
        {
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
        if (_client == null) return;

        var list = entities as IList<T> ?? entities.ToList();
        if (list.Count == 0) return;

        if (!TryGetIndexName(list[0]!.GetType(), tenantId, out var indexName)) return;

        try
        {
            var response = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(list)
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

    public Task DeleteEntityAsync<T>(string tenantId, string id) where T : class
        => DeleteEntityAsync(tenantId, typeof(T), id);

    /// <summary>
    /// Type-taking overload, because the caller that matters - SearchSyncInterceptor handling a
    /// delete - only has the runtime type. It previously passed `object`, which built
    /// "{tenant}_object" and so deleted nothing from any index a search would read.
    /// </summary>
    public async Task DeleteEntityAsync(string tenantId, Type entityType, string id)
    {
        if (_client == null) return;
        if (!TryGetIndexName(entityType, tenantId, out var indexName)) return;

        try
        {
            await _client.DeleteAsync(indexName, id);
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

    // S1: Bootstrap per-tenant indexes with proper field mappings
    public async Task EnsureTenantIndexesAsync(string tenantId)
    {
        if (_client == null) return;

        foreach (var suffix in IndexSuffixByType.Values)
        {
            var idxName = $"{tenantId}_{suffix}";
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
        if (_client == null) return Enumerable.Empty<T>();
        if (!TryGetIndexName(typeof(T), tenantId, out var indexName)) return Enumerable.Empty<T>();

        try
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(indexName)
                .Query(BuildTextQuery<T>(query)),
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
        if (_client == null) return Array.Empty<SearchHit>();

        try
        {
            var indices = IndexSuffixByType.Values.Select(s => $"{tenantId}_{s}").ToArray();

            var response = await _client.SearchAsync<SearchHit>(s => s
                .Indices(indices)
                .Query(BuildTextQuery<SearchHit>(query))
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

    // S2: Autocomplete/typeahead with prefix matching
    public async Task<IEnumerable<Upkilo.Core.Interfaces.AutocompleteSuggestion>> AutocompleteAsync(
        string tenantId, string prefix, string[]? types = null)
    {
        if (_client == null) return Enumerable.Empty<Upkilo.Core.Interfaces.AutocompleteSuggestion>();

        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
            return Enumerable.Empty<Upkilo.Core.Interfaces.AutocompleteSuggestion>();

        // The caller supplies ?type= and it lands in an index NAME, so it is filtered against
        // the known set rather than interpolated as given. The tenant prefix always bounded this
        // to the caller's own data, so it was never a tenant escape — but "whatever the client
        // sent" has no business reaching index resolution, and an unknown value would otherwise
        // produce an index_not_found error per request.
        var requested = (types is { Length: > 0 })
            ? types.Where(t => KnownSuffixes.Contains(t)).ToArray()
            : IndexSuffixByType.Values.ToArray();

        if (requested.Length == 0)
            return Enumerable.Empty<Upkilo.Core.Interfaces.AutocompleteSuggestion>();

        try
        {
            var indices = requested.Select(t => $"{tenantId}_{t}").ToArray();

            var response = await _client.SearchAsync<AutocompleteHit>(s => s
                .Indices(indices)
                .Query(q => q.MultiMatch(mm => mm
                    .Query(prefix)
                    .Fields(SearchableFields)
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

    /// <summary>
    /// Builds the text query for free-text search.
    ///
    /// This was previously a query_string query with the caller's input interpolated straight
    /// into it:
    ///
    ///     .Query(q =&gt; q.QueryString(qs =&gt; qs.Query($"*{query}*")))
    ///
    /// query_string is a full query LANGUAGE - field selectors, boolean operators, ranges,
    /// regex, wildcards - so the caller was writing part of the query, not supplying a term.
    /// That let a search reach fields it was never meant to (`email:*`, `*:*`) and let a
    /// crafted regex or leading wildcard burn CPU on demand. It stayed inside the caller's own
    /// tenant indices, so it was never a tenant escape, but it is injection all the same.
    ///
    /// multi_match takes the input as a VALUE against an explicit field list, so the input can
    /// no longer be syntax. It also drops the forced leading "*" that made every single search
    /// a full-index scan.
    /// </summary>
    private static Action<Elastic.Clients.Elasticsearch.QueryDsl.QueryDescriptor<T>> BuildTextQuery<T>(string query)
        => q => q.MultiMatch(mm => mm
            .Query(query)
            .Fields(SearchableFields)
            .Type(Elastic.Clients.Elasticsearch.QueryDsl.TextQueryType.BestFields)
            .Fuzziness(new Elastic.Clients.Elasticsearch.Fuzziness("AUTO")));

    private static bool TryGetIndexName(Type entityType, string tenantId, out string indexName)
    {
        if (IndexSuffixByType.TryGetValue(entityType, out var suffix))
        {
            indexName = $"{tenantId}_{suffix}";
            return true;
        }

        indexName = string.Empty;
        return false;
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
        public string? FullName { get; set; }
    }
}
