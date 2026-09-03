using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Upkilo.Core.Interfaces;
using Asp.Versioning;

namespace Upkilo.API.Controllers;

[Authorize]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly IElasticsearchService _searchService;
    private readonly Upkilo.Infrastructure.Services.SearchEnhancementService _searchEnhancementService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IElasticsearchService searchService,
        Upkilo.Infrastructure.Services.SearchEnhancementService searchEnhancementService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _searchEnhancementService = searchEnhancementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GlobalSearch(
        [FromQuery] string q,
        [FromQuery] string? type = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Search query cannot be empty");

        var tenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized();

        try
        {
            var results = await _searchService.GlobalSearchAsync(tenantId, q);

            // Log recent search asynchronously without awaiting if possible, but await for simplicity
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid) && Guid.TryParse(tenantId, out var tid))
            {
                await _searchEnhancementService.LogSearchAsync(tid, uid, q, type ?? "Global", 10);
            }

            return Ok(new { results, degraded = !_searchService.IsAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Global search failed for query: {Query}", q);
            return StatusCode(500, "Internal server error during search");
        }
    }

    /// <summary>
    /// S2: Autocomplete/typeahead — fuzzy prefix matching, returns ranked suggestions in &lt; 100ms.
    /// GET /search/autocomplete?q=mass&amp;type=services,businesses
    /// </summary>
    /// <remarks>
    /// Anonymous by design (public/marketplace typeahead), and therefore rate-limited: an
    /// unauthenticated fuzzy-search endpoint with no limit is a free way to make the API do
    /// expensive work. Uses the existing "public" policy rather than inventing another.
    /// </remarks>
    [HttpGet("autocomplete")]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] string? type = null)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { suggestions = Array.Empty<object>(), degraded = !_searchService.IsAvailable });

        var tenantId = User.FindFirst("tenant_id")?.Value ?? "public";
        var types = string.IsNullOrEmpty(type)
            ? null
            : type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        try
        {
            var suggestions = await _searchService.AutocompleteAsync(tenantId, q, types);
            return Ok(new
            {
                query = q,
                suggestions = suggestions.Select(s => new { s.Id, s.Text, s.Type, s.Score }),
                // "no matches" and "search is switched off" both returned an empty list, so a
                // caller could not tell them apart and neither could anyone reading a bug report.
                degraded = !_searchService.IsAvailable
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autocomplete failed for query: {Query}", q);
            return Ok(new { suggestions = Array.Empty<object>(), degraded = true });
        }
    }

    /// <summary>
    /// Auto-complete search suggestions (legacy — use /autocomplete)
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { suggestions = Array.Empty<object>() });

        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized();

        try
        {
            var results = await _searchService.GlobalSearchAsync(tenantId, q);
            return Ok(new { suggestions = results, degraded = !_searchService.IsAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search suggestions failed for query: {Query}", q);
            return Ok(new { suggestions = Array.Empty<object>(), degraded = true });
        }
    }

    /// <summary>
    /// S1: Bootstrap Elasticsearch indexes for this tenant with proper field mappings.
    /// POST /search/bootstrap-indexes
    ///
    /// Restricted to Owner/Admin. It was reachable by ANY authenticated user on the tenant —
    /// index creation is an administrative operation, and the reindex endpoint below even
    /// documented itself as "admin only" while enforcing nothing.
    /// </summary>
    [HttpPost("bootstrap-indexes")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> BootstrapIndexes()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized();

        try
        {
            await _searchService.EnsureTenantIndexesAsync(tenantId);
            _logger.LogInformation("[S1] Elasticsearch indexes bootstrapped for tenant {TenantId}", tenantId);

            return Ok(new
            {
                status = "created",
                tenantId,
                indexes = new[] { $"{tenantId}_clients", $"{tenantId}_bookings", $"{tenantId}_services" },
                message = "Elasticsearch indexes created with proper field mappings."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index bootstrap failed for tenant {TenantId}", tenantId);
            return StatusCode(500, "Index bootstrap failed");
        }
    }

    /// <summary>
    /// Trigger reindexing of all tenant data. Admin only — now actually enforced.
    /// </summary>
    [HttpPost("reindex")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> TriggerReindex()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
            return Unauthorized();

        try
        {
            await _searchService.EnsureTenantIndexesAsync(tenantId);
            _logger.LogInformation("Reindex triggered for tenant {TenantId}", tenantId);

            return Ok(new
            {
                status = "queued",
                message = "Reindexing has been queued and will complete shortly.",
                tenantId,
                requestedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reindex failed for tenant {TenantId}", tenantId);
            return StatusCode(500, "Reindex operation failed");
        }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentSearches([FromQuery] int limit = 5)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId) ||
            !Guid.TryParse(tenantId, out var tid) || !Guid.TryParse(userId, out var uid))
            return Unauthorized();

        var recent = await _searchEnhancementService.GetRecentSearchesAsync(tid, uid, limit);
        return Ok(recent);
    }

    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedSearches()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId) ||
            !Guid.TryParse(tenantId, out var tid) || !Guid.TryParse(userId, out var uid))
            return Unauthorized();

        var saved = await _searchEnhancementService.GetSavedSearchesAsync(tid, uid);
        return Ok(saved);
    }

    public class SaveSearchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string SearchType { get; set; } = "Global";
        public string FiltersJson { get; set; } = "{}";
    }

    [HttpPost("saved")]
    public async Task<IActionResult> SaveSearchFilter([FromBody] SaveSearchRequest req)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId) ||
            !Guid.TryParse(tenantId, out var tid) || !Guid.TryParse(userId, out var uid))
            return Unauthorized();

        var saved = await _searchEnhancementService.SaveSearchAsync(tid, uid, req.Name, req.Query, req.SearchType, req.FiltersJson);
        return Ok(saved);
    }

    [HttpDelete("saved/{id}")]
    public async Task<IActionResult> DeleteSavedSearch(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId) ||
            !Guid.TryParse(tenantId, out var tid) || !Guid.TryParse(userId, out var uid))
            return Unauthorized();

        await _searchEnhancementService.DeleteSavedSearchAsync(id, tid, uid);
        return NoContent();
    }
}
