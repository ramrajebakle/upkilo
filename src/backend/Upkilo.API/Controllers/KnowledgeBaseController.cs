using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
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
    // Entries live in the AIKnowledgeBases table, which is the SAME store the assistant reads
    // through ChatbotContextBuilder.
    //
    // They previously lived in a pair of static ConcurrentDictionaries. That made this page a
    // dead end in three separate ways: nothing written here ever reached the assistant (the
    // prompt is built from AIKnowledgeBases, which this controller never touched), everything
    // was lost on restart, and nothing was visible to any other replica. A user training the
    // bot on the page named "Knowledge Base" got silence, with no error to explain it.
    //
    // Every query below filters on the tenant id from the authenticated principal explicitly,
    // rather than relying on the global query filter - the same reasoning as ChatbotContextBuilder.

    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        ILogger<KnowledgeBaseController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------
    private Guid GetTenantId() =>
        _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    private async Task<List<KbEntry>> TenantEntriesAsync(Guid tenantId) =>
        (await _db.AIKnowledgeBases
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId && !k.IsDeleted)
            .ToListAsync())
        .Select(ToKbEntry)
        .ToList();

    /// <summary>
    /// Maps the stored row to the wire shape. The two now use the same field names, so this is a
    /// straight projection rather than a translation.
    /// </summary>
    private static KbEntry ToKbEntry(AIKnowledgeBase k) => new(
        Id: k.Id,
        TenantId: k.TenantId,
        Question: k.Question,
        Answer: k.Answer,
        Category: k.Category,
        Tags: DeserialiseTags(k.Tags),
        Embedding: k.VectorEmbedding,
        CreatedAt: k.CreatedAt,
        UpdatedAt: k.UpdatedAt
    );

    // Tags are a string[] on the wire but a single nullable column in the table. JSON rather than
    // a comma-join so a tag containing a comma survives the round trip.
    private static string? SerialiseTags(string[] tags) =>
        tags.Length == 0 ? null : JsonSerializer.Serialize(tags);

    private static string[] DeserialiseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<string[]>(raw) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            // Rows written by another path (or by hand) may hold a plain comma list. Falling back
            // keeps those readable instead of failing the whole listing.
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    // ---------------------------------------------------------------------------
    // GET /knowledge-base/entries
    // ---------------------------------------------------------------------------
    /// <summary>List all knowledge-base entries for the current tenant, with optional type/search filter.</summary>
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] string? type,
        [FromQuery] string? search)
    {
        var tenantId = GetTenantId();
        IEnumerable<KbEntry> entries = await TenantEntriesAsync(tenantId);

        if (!string.IsNullOrWhiteSpace(type))
            entries = entries.Where(e => e.Category.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            entries = entries.Where(e =>
                e.Question.ToLowerInvariant().Contains(s) ||
                e.Answer.ToLowerInvariant().Contains(s) ||
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
    public async Task<IActionResult> AddEntry([FromBody] KbEntryRequest request)
    {
        var tenantId = GetTenantId();

        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(ApiResponse<object>.Fail("Question is required"));
        if (string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(ApiResponse<object>.Fail("Answer is required"));

        // Bounded for the same reason as AIChatbotController.Train: this text is copied verbatim
        // into the assistant's system prompt as its highest-ranked source, so an unbounded entry
        // is a per-turn cost and context-window problem, not just a wide column.
        if (request.Question.Length > 500 || request.Answer.Length > 2000)
            return BadRequest(ApiResponse<object>.Fail(
                "Question must be under 500 characters and answer under 2000."));

        var row = new AIKnowledgeBase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Question = request.Question.Trim(),
            Answer = request.Answer.Trim(),
            Category = NormaliseType(request.Category),
            Tags = SerialiseTags(NormaliseTags(request.Tags)),
            IsActive = true
        };

        _db.AIKnowledgeBases.Add(row);
        await _db.SaveChangesAsync();

        _logger.LogInformation("KB entry {Id} created for tenant {TenantId}", row.Id, tenantId);

        return CreatedAtAction(nameof(GetEntries), new { }, ApiResponse<KbEntry>.Ok(ToKbEntry(row)));
    }

    // ---------------------------------------------------------------------------
    // PUT /knowledge-base/entries/{id}
    // ---------------------------------------------------------------------------
    /// <summary>Update an existing knowledge-base entry.</summary>
    [HttpPut("entries/{id:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid id, [FromBody] KbEntryRequest request)
    {
        var tenantId = GetTenantId();

        // Matched on id AND tenant, so an id belonging to another tenant is a 404 rather than a
        // cross-tenant write.
        var existing = await _db.AIKnowledgeBases
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId && !k.IsDeleted);

        if (existing == null)
            return NotFound(ApiResponse<object>.Fail("Entry not found"));

        if (request.Question?.Length > 500 || request.Answer?.Length > 2000)
            return BadRequest(ApiResponse<object>.Fail(
                "Question must be under 500 characters and answer under 2000."));

        if (!string.IsNullOrWhiteSpace(request.Question)) existing.Question = request.Question.Trim();
        if (!string.IsNullOrWhiteSpace(request.Answer)) existing.Answer = request.Answer.Trim();
        existing.Category = NormaliseType(request.Category);
        existing.Tags = SerialiseTags(NormaliseTags(request.Tags));
        existing.VectorEmbedding = null; // invalidate embedding on update
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<KbEntry>.Ok(ToKbEntry(existing)));
    }

    // ---------------------------------------------------------------------------
    // DELETE /knowledge-base/entries/{id}
    // ---------------------------------------------------------------------------
    /// <summary>Delete a knowledge-base entry.</summary>
    [HttpDelete("entries/{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var tenantId = GetTenantId();

        var existing = await _db.AIKnowledgeBases
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId && !k.IsDeleted);

        if (existing == null)
            return NotFound(ApiResponse<object>.Fail("Entry not found"));

        _db.AIKnowledgeBases.Remove(existing);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Entry deleted"));
    }

    // ---------------------------------------------------------------------------
    // POST /knowledge-base/entries/bulk-import
    // ---------------------------------------------------------------------------
    /// <summary>Bulk-import entries from a JSON array.</summary>
    [HttpPost("entries/bulk-import")]
    public async Task<IActionResult> BulkImport([FromBody] IEnumerable<KbEntryRequest> requests)
    {
        var tenantId = GetTenantId();
        var importList = requests?.ToList();

        if (importList == null || importList.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("No entries provided"));

        var created = new List<AIKnowledgeBase>();
        var errors = new List<string>();
        var index = 0;

        foreach (var req in importList)
        {
            index++;
            if (string.IsNullOrWhiteSpace(req.Question) || string.IsNullOrWhiteSpace(req.Answer))
            {
                errors.Add($"Entry {index}: question and answer are required");
                continue;
            }

            if (req.Question.Length > 500 || req.Answer.Length > 2000)
            {
                errors.Add($"Entry {index}: question must be under 500 characters and answer under 2000");
                continue;
            }

            created.Add(new AIKnowledgeBase
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Question = req.Question.Trim(),
                Answer = req.Answer.Trim(),
                Category = NormaliseType(req.Category),
                Tags = SerialiseTags(NormaliseTags(req.Tags)),
                IsActive = true
            });
        }

        if (created.Count > 0)
        {
            _db.AIKnowledgeBases.AddRange(created);
            await _db.SaveChangesAsync();
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
    public async Task<IActionResult> TriggerTraining()
    {
        var tenantId = GetTenantId();
        var jobId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var count = await _db.AIKnowledgeBases
            .CountAsync(k => k.TenantId == tenantId && !k.IsDeleted && k.IsActive);

        // There is no embedding/vector step to queue: the assistant reads these rows directly
        // when it builds a prompt (ChatbotContextBuilder), so a saved entry is live on the very
        // next message. The old copy promised re-indexing "in approximately 2 minutes", which
        // described work that never happened and told the user to wait for nothing.
        _logger.LogInformation("KB train requested for tenant {TenantId} ({Count} active entries)", tenantId, count);

        return Accepted(ApiResponse<TrainJobResult>.Ok(new TrainJobResult(
            JobId: jobId,
            Status: "completed",
            Message: count == 0
                ? "No active knowledge base entries yet. Add some and your assistant will use them immediately."
                : $"Your assistant is using all {count} active entries. Changes take effect on the next message.",
            QueuedAt: now
        )));
    }

    // ---------------------------------------------------------------------------
    // GET /knowledge-base/search?q=
    // ---------------------------------------------------------------------------
    /// <summary>Semantic search using AI to rank relevant entries for the query.</summary>
    [HttpGet("search")]
    // IAIService is injected per-action; see ServicesController for why a constructor
    // dependency here made every endpoint on this controller construct the AI stack.
    public async Task<IActionResult> Search([FromQuery] string q, [FromServices] IAIService aiService)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(ApiResponse<object>.Fail("Query parameter 'q' is required"));

        var tenantId = GetTenantId();
        var userId = _tenantProvider.GetUserId();
        var entries = await TenantEntriesAsync(tenantId);

        if (entries.Count == 0)
            return Ok(ApiResponse<IEnumerable<KbEntry>>.Ok(Enumerable.Empty<KbEntry>()));

        // Build a compact representation of all entries for the AI context window
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("You are a knowledge base search assistant. Given the following entries and a user query, return a JSON array of the IDs of the most relevant entries (most relevant first, max 5). Only return the JSON array, nothing else.");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("### ENTRIES ###");
        foreach (var e in entries)
            contextBuilder.AppendLine($"ID:{e.Id} TYPE:{e.Category} TITLE:{e.Question} CONTENT:{e.Answer[..Math.Min(e.Answer.Length, 300)]}");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine($"### USER QUERY ###");
        contextBuilder.AppendLine(q);
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Return ONLY a JSON array of GUIDs, e.g.: [\"id1\",\"id2\"]");

        var aiResult = await aiService.GenerateTextAsync(tenantId, userId, contextBuilder.ToString());

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
    // IAIService is injected per-action; see ServicesController for why a constructor
    // dependency here made every endpoint on this controller construct the AI stack.
    public async Task<IActionResult> AutoPopulate([FromServices] IAIService aiService)
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

        var aiResult = await aiService.GenerateTextAsync(tenantId, userId, promptBuilder.ToString());
        if (!aiResult.Success || string.IsNullOrWhiteSpace(aiResult.Content))
            return StatusCode(502, ApiResponse<object>.Fail("AI service failed to generate FAQ entries. Please try again."));

        var created = new List<AIKnowledgeBase>();
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

                    // The model produced these, so they are bounded here rather than trusted -
                    // an over-long generated answer would otherwise be persisted and then
                    // replayed into every future prompt.
                    if (dto.Title.Length > 500 || dto.Content.Length > 2000) continue;

                    created.Add(new AIKnowledgeBase
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Question = dto.Title.Trim(),
                        Answer = dto.Content.Trim(),
                        Category = NormaliseType(dto.Type),
                        Tags = SerialiseTags(NormaliseTags(dto.Tags)),
                        IsActive = true
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI auto-populate response for tenant {TenantId}", tenantId);
            return StatusCode(502, ApiResponse<object>.Fail("Failed to parse AI response. Please try again."));
        }

        if (created.Count > 0)
        {
            _db.AIKnowledgeBases.AddRange(created);
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Auto-populated {Count} KB entries for tenant {TenantId}", created.Count, tenantId);
        return Ok(ApiResponse<IEnumerable<KbEntry>>.Ok(
            created.Select(ToKbEntry), $"{created.Count} FAQ entries generated from your services"));
    }

    // ---------------------------------------------------------------------------
    // GET /knowledge-base/stats
    // ---------------------------------------------------------------------------
    /// <summary>Return entry counts by type and last-trained timestamp.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = GetTenantId();
        var entries = await TenantEntriesAsync(tenantId);

        var byType = entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var stats = new KbStats(
            TotalEntries: entries.Count,
            ByType: byType,
            // Derived from the newest entry rather than read from a separate counter. The counter
            // was a static dictionary that emptied on restart, so this figure blanked itself
            // periodically for no reason the user could see. Since the assistant reads these rows
            // live, "last changed" is also the honest answer to "last trained".
            LastTrainedAt: entries.Count == 0 ? null : entries.Max(e => e.UpdatedAt)
        );

        return Ok(ApiResponse<KbStats>.Ok(stats));
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------
    /// <summary>
    /// Category is free text, trimmed, defaulting to "General".
    ///
    /// It used to be coerced to one of faq/service/policy/custom, with everything else silently
    /// becoming "custom". Nothing justified that: the page offers a free-text category box, and
    /// the other writer over this same table (AIChatbotController.Train) stores whatever the user
    /// typed. So a category of "Pricing" was accepted by the form and then quietly filed as
    /// "custom", and re-saving an entry created on the chatbot page relabelled it.
    /// </summary>
    private static string NormaliseType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? "General" : type.Trim();

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
                    (e.Question.ToLowerInvariant().Contains(t) ? 3 : 0) +
                    (e.Answer.ToLowerInvariant().Contains(t) ? 1 : 0) +
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

/// <summary>
/// A knowledge-base entry as sent to and from the browser.
///
/// Named Question/Answer/Category to match both the AIKnowledgeBase row behind it and the other
/// endpoint over the same table (AIChatbotController's /aichatbot/kb). These fields were
/// previously Title/Content/Type, which matched neither: the knowledge base page reads
/// entry.question and posts { question, answer }, so every listed entry rendered its fields as
/// undefined and every create was rejected with "Title is required". Two endpoints over one table
/// disagreeing about its field names is what produced that.
/// </summary>
public record KbEntry(
    Guid Id,
    Guid TenantId,
    string Question,
    string Answer,
    string Category,
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
    // Matches what the knowledge base page actually posts: { question, answer, category, tags }.
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Category { get; set; }
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
