using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

/// <summary>
/// Content Moderation API — analyze text for harmful content via
/// Azure AI Content Safety with heuristic fallback.
/// Uses the existing IContentModerationService (Upkilo.Core.Interfaces).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/moderation")]
[Authorize]
public class ContentModerationController : ControllerBase
{
    private readonly IContentModerationService _moderator;
    private readonly ILogger<ContentModerationController> _logger;

    public ContentModerationController(
        IContentModerationService moderator,
        ILogger<ContentModerationController> logger)
    {
        _moderator = moderator;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/moderation/text
    // Analyze a text string for harmful content
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("text")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeText(
        [FromBody] TextModerationRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(ApiResponse<object>.Fail("Text is required"));

        var result = await _moderator.ModerateTextAsync(req.Text, req.SeverityThreshold, ct);

        _logger.LogInformation(
            "Content moderation: allowed={Allowed} flagged={Flagged}",
            result.IsAllowed, result.FlaggedCategories.Count);

        return Ok(ApiResponse<object>.Ok(new
        {
            isAllowed = result.IsAllowed,
            isBlocked = !result.IsAllowed,
            flaggedCategories = result.FlaggedCategories,
            rawScores = result.RawScores
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/moderation/sanitize
    // Analyze AND sanitize an AI output — strips or blocks harmful content
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("sanitize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ModerateAndSanitize(
        [FromBody] TextModerationRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(ApiResponse<object>.Fail("Text is required"));

        var (sanitized, result) = await _moderator.ModerateAndSanitizeAsync(req.Text, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            sanitized,
            isAllowed = result.IsAllowed,
            flaggedCategories = result.FlaggedCategories
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/moderation/batch
    // Analyze multiple text entries (e.g. bulk user reviews, messages)
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeBatch(
        [FromBody] BatchModerationRequest req,
        CancellationToken ct)
    {
        if (req.Items == null || req.Items.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Items required"));

        if (req.Items.Count > 50)
            return BadRequest(ApiResponse<object>.Fail("Maximum 50 items per batch"));

        var tasks = req.Items.Select(item =>
            _moderator.ModerateTextAsync(item.Text, ct: ct));

        var results = await Task.WhenAll(tasks);

        var blockedCount = results.Count(r => !r.IsAllowed);

        return Ok(ApiResponse<object>.Ok(new
        {
            total = results.Length,
            blocked = blockedCount,
            allowed = results.Length - blockedCount,
            results = results.Zip(req.Items, (r, item) => new
            {
                id = item.Id,
                isAllowed = r.IsAllowed,
                isBlocked = !r.IsAllowed,
                flaggedCount = r.FlaggedCategories.Count
            })
        }));
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record TextModerationRequest
{
    public string Text { get; init; } = "";
    public string? AuthorId { get; init; }
    public int? SeverityThreshold { get; init; }
}

public record BatchModerationRequest
{
    public List<BatchItem> Items { get; init; } = new();
}

public record BatchItem
{
    public string Id { get; init; } = "";
    public string Text { get; init; } = "";
}
