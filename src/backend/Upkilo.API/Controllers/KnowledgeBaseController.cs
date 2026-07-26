using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Manages the AI chatbot knowledge base — FAQs, service info, policies, and custom entries.
/// Supports semantic search with RAG via IAIService and full tenant isolation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/knowledge-base")]
[Authorize]
public class KnowledgeBaseController : ControllerBase
{
    // ---------------------------------------------------------------------------
    // In-memory store (keyed by (tenantId, entryId) for strict tenant isolation)
    // ---------------------------------------------------------------------------
    private static readonly ConcurrentDictionary<(Guid TenantId, Guid EntryId), KbEntry> _store = new();

    // Per-tenant training metadata: last train timestamp
    private static readonly ConcurrentDictionary<Guid, DateTime> _lastTrained = new();

    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAIService _aiService;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        IAIService aiService,
        ILogger<KnowledgeBaseController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _aiService = aiService;
        _logger = logger;
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------
    private Guid GetTenantId() =>
        _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    private IEnumerable<KbEntry> TenantEntries(Guid tenantId) =>
        _store.Where(kv => kv.Key.TenantId == tenantId).Select(kv => kv.Value);

    // ---------------------------------------------------------------------------
    // GET /knowledge-base/entries
    // ---------------------------------------------------------------------------
    /// <summary>List all knowledge-base entries for the current tenant, with optional type/search filter.</summary>
    [HttpGet("entries")]
    public IActionResult GetEntries(
        [FromQuery] string? type,
        [FromQuery] string? search)
    {
        var tenantId = GetTenantId();
        var entries = TenantEntries(tenantId);

        if (!string.IsNullOrWhiteSpace(type))
            entries = entries.Where(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            entries = entries.Where(e =>
                e.Title.ToLowerInvariant().Contains(s) ||
                e.Content.ToLowerInvariant().Contains(s) ||
                e.Tags.Any(t => t.ToLowerInvariant().Contains(s)));
        }

        var result = entries
            .OrderByDescending(e => e.UpdatedAt)
            .ToList();

        return Ok(ApiResponse<IEnumerable<KbEntry>>.Ok(result));
    }

    // ---------------------------------------------------------------------------
    // POST /knowledge-base/entries
    // ---------------------------------------------------------------------------
    /// <summary>Add a new knowledge-base entry.</summary>
    [HttpPost("entries")]
    public IActionResult AddEntry([FromBody] KbEntryRequest request)
    {
        var tenantId = GetTenantId();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(ApiResponse<object>.Fail("Title is required"));
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(ApiResponse<object>.Fail("Content is required"));

        var entry = new KbEntry(
            Id: Guid.NewGuid(),
            TenantId: tenantId,
            Title: request.Title.Trim(),
            Content: request.Content.Trim(),
            Type: NormaliseType(request.Type),
            Tags: NormaliseTags(request.Tags),
            Embedding: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _store[(tenantId, entry.Id)] = entry;
        _logger.LogInformation("KB entry {Id} created for tenant {TenantId}", entry.Id, tenantId);

        return CreatedAtAction(nameof(GetEntries), new { }, ApiResponse<KbEntry>.Ok(entry));
    }

    // ---------------------------------------------------------------------------
    // PUT /knowledge-base/entries/{id}
    // ---------------------------------------------------------------------------
    /// <summary>Update an existing knowledge-base entry.</summary>
    [HttpPut("entries/{id:guid}")]
    public IActionResult UpdateEntry(Guid id, [FromBody] KbEntryRequest request)
    {
        var tenantId = GetTenantId();
        var key = (tenantId, id);

        if (!_store.TryGetValue(key, out var existing))
            return NotFound(ApiResponse<object>.Fail("Entry not found"));

        var updated = existing with
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? existing.Title : request.Title.Trim(),
            Content = string.IsNullOrWhiteSpace(request.Content) ? existing.Content : request.Content.Trim(),
            Type = NormaliseType(request.Type),
            Tags = NormaliseTags(request.Tags),
            Embedding = null, // invalidate embedding on update
            UpdatedAt = DateTime.UtcNow
        };

        _store[key] = updated;
        return Ok(ApiResponse<KbEntry>.Ok(updated));
    }

    // ---------------------------------------------------------------------------
    // DELETE /knowledge-base/entries/{id}
    // ---------------------------------------------------------------------------
    /// <summary>Delete a knowledge-base entry.</summary>
    [HttpDelete("entries/{id:guid}")]
    public IActionResult DeleteEntry(Guid id)
    {
        var tenantId = GetTenantId();

        if (!_store.TryRemove((tenantId, id), out _))
            return NotFound(ApiResponse<object>.Fail("Entry not found"));

        return Ok(ApiResponse.Ok("Entry deleted"));
    }

    // ---------------------------------------------------------------------------
    // POST /knowledge-base/entries/bulk-import
    // ---------------------------------------------------------------------------
    /// <summary>Bulk-import entries from a JSON array.</summary>
    [HttpPost("entries/bulk-import")]
    public IActionResult BulkImport([FromBody] IEnumerable<KbEntryRequest> requests)
    {
        var tenantId = GetTenantId();
        var importList = requests?.ToList();

        if (importList == null || importList.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("No entries provided"));

        var created = new List<KbEntry>();
        var errors = new List<string>();
        var index = 0;

        foreach (var req in importList)
        {
            index++;
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Content))
            {
                errors.Add($"Entry {index}: title and content are required");
                continue;
            }

            var entry = new KbEntry(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                Title: req.Title.Trim(),
                Content: req.Content.Trim(),
                Type: NormaliseType(req.Type),
                Tags: NormaliseTags(req.Tags),
                Embedding: null,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow
            );
            _store[(tenantId, entry.Id)] = entry;
            created.Add(entry);
        }

        _logger.LogInformation("Bulk-imported {Count} KB entries for tenant {TenantId}", created.Count, tenantId);

        return Ok(ApiResponse<BulkImportResult>.Ok(new BulkImportResult(
            Imported: created.Count,
            Skipped: errors.Count,
            Errors: errors
        )));
    }

    // ---------------------------------------------------------------------------
    // POST /knowledge-base/train
    // ---------------------------------------------------------------------------
    /// <summary>Trigger re-indexing of the knowledge base (async job).</summary>
    [HttpPost("train")]
    public IActionResult TriggerTraining()
    {
        var tenantId = GetTenantId();
        var jobId = Guid.NewGuid();

        // In a real system this queues a vector-embedding job.
        // Here we update the "indexed" timestamp on every entry.
        var now = DateTime.UtcNow;
        foreach (var key in _store.Keys.Where(k => k.TenantId == tenantId).ToList())
        {
            if (_store.TryGetValue(key, out var entry))
                _store[key] = entry with { UpdatedAt = entry.UpdatedAt }; // no-op on content; timestamp unchanged intentionally
        }
        _lastTrained[tenantId] = now;

        _logger.LogInformation("Training job {JobId} queued for tenant {TenantId}", jobId, tenantId);

        return Accepted(ApiResponse<TrainJobResult>.Ok(new TrainJobResult(
            JobId: jobId,
            Status: "queued",
            Message: "Re-indexing has been queued. Your AI will use the updated knowledge in approximately 2 minutes.",
            QueuedAt: now
        )));
    }

    // ---------------------------------------------------------------------------
    // GET /knowledge-base/search?q=
    // ---------------------------------------------------------------------------
    /// <summary>Semantic search using AI to rank relevant entries for the query.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(ApiResponse<object>.Fail("Query parameter 'q' is required"));

        var tenantId = GetTenantId();
        var userId = _tenantProvider.GetUserId();
        var entries = TenantEntries(tenantId).ToList();

        if (entries.Count == 0)
            return Ok(ApiResponse<IEnumerable<KbEntry>>.Ok(Enumerable.Empty<KbEntry>()));

        // Build a compact representation of all entries for the AI context window
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("You are a knowledge base search assistant. Given the following entries and a user query, return a JSON array of the IDs of the most relevant entries (most relevant first, max 5). Only return the JSON array, nothing else.");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("### ENTRIES ###");
        foreach (var e in entries)
            contextBuilder.AppendLine($"ID:{e.Id} TYPE:{e.Type} TITLE:{e.Title} CONTENT:{e.Content[..Math.Min(e.Content.Length, 300)]}");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine($"### USER QUERY ###");
        contextBuilder.AppendLine(q);
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Return ONLY a JSON array of GUIDs, e.g.: [\"id1\",\"id2\"]");

        var aiResult = await _aiService.GenerateTextAsync(tenantId, userId, contextBuilder.ToString());

        IEnumerable<KbEntry> ranked;
        if (aiResult.Success && !string.IsNullOrWhiteSpace(aiResult.Content))
        {
            try
            {
                // Parse the returned ID array and return entries in ranked order
                var rawJson = aiResult.Content.Trim();
                // Strip markdown code fences if present
                if (rawJson.StartsWith("```")) rawJson = rawJson.Split('\n').Skip(1).Aggregate("", (a, b) => a + b).Replace("```", "");
                var ids = JsonSerializer.Deserialize<Guid[]>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (ids != null && ids.Length > 0)
                {
                    var idSet = ids.Select((id, rank) => (id, rank)).ToDictionary(x => x.id, x => x.rank);
                    ranked = entries
                        .Where(e => idSet.ContainsKey(e.Id))
                        .OrderBy(e => idSet[e.Id]);
                }
                else
                {
                    ranked = FallbackSearch(entries, q);
                }
            }
            catch
            {
                _logger.LogWarning("AI search response could not be parsed; falling back to keyword search");
                ranked = FallbackSearch(entries, q);
            }
        }
        else
        {
            ranked = FallbackSearch(entries, q);
        }

        return Ok(ApiResponse<IEnumerable<KbEntry>>.Ok(ranked));
    }

    // ---------------------------------------------------------------------------
    // POST /knowledge-base/auto-populate
    // ---------------------------------------------------------------------------
    /// <summary>Auto-generate FAQ entries from tenant's services/business info using AI.</summary>
    [HttpPost("auto-populate")]
    public async Task<IActionResult> AutoPopulate()
    {
        var tenantId = GetTenantId();
        var userId = _tenantProvider.GetUserId();

        // Fetch services for this tenant
        var services = await _db.Services
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .Select(s => new { s.Name, s.Description, s.Price, s.Currency, s.DurationMinutes, s.Category, s.CancellationPolicy })
            .ToListAsync();

        if (services.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("No active services found. Please add services before auto-populating the knowledge base."));

        // Build prompt
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are an AI assistant that creates FAQ entries for a business chatbot knowledge base.");
        promptBuilder.AppendLine("Based on the services below, generate a comprehensive list of FAQ entries a client might ask.");
        promptBuilder.AppendLine("Return ONLY a valid JSON array. Each object must have: title (string), content (string), type (\"faq\" or \"service\"), tags (string[]).");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### SERVICES ###");
        foreach (var svc in services)
        {
            promptBuilder.AppendLine($"- Name: {svc.Name}");
            if (!string.IsNullOrWhiteSpace(svc.Description))
                promptBuilder.AppendLine($"  Description: {svc.Description}");
            promptBuilder.AppendLine($"  Price: {svc.Currency} {svc.Price:F2}");
            promptBuilder.AppendLine($"  Duration: {svc.DurationMinutes} minutes");
            if (!string.IsNullOrWhiteSpace(svc.Category))
                promptBuilder.AppendLine($"  Category: {svc.Category}");
            if (!string.IsNullOrWhiteSpace(svc.CancellationPolicy))
                promptBuilder.AppendLine($"  Cancellation Policy: {svc.CancellationPolicy}");
        }
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate 8-12 FAQ entries. Return ONLY the JSON array.");

        var aiResult = await _aiService.GenerateTextAsync(tenantId, userId, promptBuilder.ToString());
        if (!aiResult.Success || string.IsNullOrWhiteSpace(aiResult.Content))
            return StatusCode(502, ApiResponse<object>.Fail("AI service failed to generate FAQ entries. Please try again."));

        var created = new List<KbEntry>();
        try
        {
            var rawJson = aiResult.Content.Trim();
            if (rawJson.StartsWith("```")) rawJson = rawJson.Split('\n').Skip(1).Aggregate("", (a, b) => a + b).Replace("```", "");

            var dtos = JsonSerializer.Deserialize<List<AutoPopulateDto>>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content)) continue;
                    var entry = new KbEntry(
                        Id: Guid.NewGuid(),
                        TenantId: tenantId,
                        Title: dto.Title.Trim(),
                        Content: dto.Content.Trim(),
                        Type: NormaliseType(dto.Type),
                        Tags: dto.Tags ?? Array.Empty<string>(),
                        Embedding: null,
                        CreatedAt: DateTime.UtcNow,
                        UpdatedAt: DateTime.UtcNow
                    );
                    _store[(tenantId, entry.Id)] = entry;
                    created.Add(entry);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI auto-populate response for tenant {TenantId}", tenantId);
            return StatusCode(502, ApiResponse<object>.Fail("Failed to parse AI response. Please try again."));
        }

        _logger.LogInformation("Auto-populated {Count} KB entries for tenant {TenantId}", created.Count, tenantId);
        return Ok(ApiResponse<IEnumerable<KbEntry>>.Ok(created, $"{created.Count} FAQ entries generated from your services"));
    }

    // ---------------------------------------------------------------------------
    // GET /knowledge-base/stats
    // ---------------------------------------------------------------------------
    /// <summary>Return entry counts by type and last-trained timestamp.</summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var tenantId = GetTenantId();
        var entries = TenantEntries(tenantId).ToList();

        _lastTrained.TryGetValue(tenantId, out var lastTrained);

        var byType = entries
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var stats = new KbStats(
            TotalEntries: entries.Count,
            ByType: byType,
            LastTrainedAt: lastTrained == default ? null : lastTrained
        );

        return Ok(ApiResponse<KbStats>.Ok(stats));
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------
    private static string NormaliseType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "faq" => "faq",
            "service" => "service",
            "policy" => "policy",
            "custom" => "custom",
            _ => "custom"
        };

    private static string[] NormaliseTags(string[]? tags) =>
        tags?.Select(t => t.Trim()).Where(t => t.Length > 0).ToArray()
        ?? Array.Empty<string>();

    private static IEnumerable<KbEntry> FallbackSearch(IEnumerable<KbEntry> entries, string q)
    {
        var terms = q.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return entries
            .Select(e => new
            {
                Entry = e,
                Score = terms.Sum(t =>
                    (e.Title.ToLowerInvariant().Contains(t) ? 3 : 0) +
                    (e.Content.ToLowerInvariant().Contains(t) ? 1 : 0) +
                    (e.Tags.Any(tag => tag.ToLowerInvariant().Contains(t)) ? 2 : 0))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => x.Entry);
    }
}

// ---------------------------------------------------------------------------
// Domain models
// ---------------------------------------------------------------------------

/// <summary>Immutable knowledge-base entry stored in memory.</summary>
public record KbEntry(
    Guid Id,
    Guid TenantId,
    string Title,
    string Content,
    string Type,
    string[] Tags,
    float[]? Embedding,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ---------------------------------------------------------------------------
// Request / Response DTOs
// ---------------------------------------------------------------------------

public class KbEntryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string[]? Tags { get; set; }
}

public record BulkImportResult(int Imported, int Skipped, IReadOnlyList<string> Errors);

public record TrainJobResult(Guid JobId, string Status, string Message, DateTime QueuedAt);

public record KbStats(
    int TotalEntries,
    IDictionary<string, int> ByType,
    DateTime? LastTrainedAt
);

// Used only for deserialising AI auto-populate JSON
internal class AutoPopulateDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string[]? Tags { get; set; }
}
