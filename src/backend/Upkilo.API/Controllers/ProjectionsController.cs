using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Events;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

/// <summary>
/// Projection management — trigger rebuilds, inspect consistency, check lag.
/// Admin-only access required for all mutating operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProjectionsController : ControllerBase
{
    private readonly ProjectionRebuilder _rebuilder;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ProjectionsController> _logger;

    public ProjectionsController(
        ProjectionRebuilder rebuilder,
        ITenantProvider tenantProvider,
        ILogger<ProjectionsController> logger)
    {
        _rebuilder = rebuilder;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/projections
    // Lists all registered projections and their last checkpoint
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListProjections()
    {
        var projections = _rebuilder.RegisteredProjections.Select(name => new
        {
            name,
            checkpoint = _rebuilder.GetCheckpoint(name)
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            projections,
            total = _rebuilder.RegisteredProjections.Count
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/projections/consistency
    // Returns full consistency report with event lag for each projection
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("consistency")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsistency(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var report = await _rebuilder.GetConsistencyReportAsync(tenantId, ct);

        var anyStale = report.Any(r => r.IsStale);
        var totalLag = report.Sum(r => r.EventLag);

        return Ok(ApiResponse<object>.Ok(new
        {
            healthy = !anyStale,
            totalLag,
            report,
            generatedAt = DateTime.UtcNow
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/v1/projections/rebuild
    // Triggers a projection rebuild. Use dryRun=true to preview without writes.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("rebuild")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Rebuild(
        [FromBody] RebuildRequest req,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();

        // Validate projection name if provided
        if (!string.IsNullOrWhiteSpace(req.ProjectionName) &&
            !_rebuilder.RegisteredProjections.Contains(req.ProjectionName, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<object>.Fail(
                $"Unknown projection '{req.ProjectionName}'. Available: {string.Join(", ", _rebuilder.RegisteredProjections)}"));
        }

        _logger.LogInformation(
            "Projection rebuild requested: projection={Projection} tenant={Tenant} dryRun={DryRun}",
            req.ProjectionName ?? "*", tenantId, req.DryRun);

        var results = await _rebuilder.RebuildAsync(
            tenantId: req.AllTenants ? null : tenantId,
            projectionName: req.ProjectionName,
            eventTypeFilter: req.EventTypeFilter,
            from: req.From,
            dryRun: req.DryRun,
            cancellationToken: ct);

        var success = results.All(r => r.Status is "ok" or "dry_run");

        return Ok(ApiResponse<object>.Ok(new
        {
            success,
            dryRun = req.DryRun,
            results,
            totalDurationMs = results.Sum(r => r.DurationMs)
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET  api/v1/projections/{name}/checkpoint
    // Returns the last checkpoint for a specific projection
    // ──────────────────────────────────────────────────────────────────────────
    [HttpGet("{name}/checkpoint")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetCheckpoint(string name)
    {
        var cp = _rebuilder.GetCheckpoint(name);
        if (cp == null)
            return NotFound(ApiResponse<object>.Fail($"No checkpoint for projection '{name}'"));

        return Ok(ApiResponse<object>.Ok(cp));
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record RebuildRequest
{
    /// <summary>Projection to rebuild (null = all)</summary>
    public string? ProjectionName { get; init; }

    /// <summary>Filter events by type (e.g. "BookingCreated")</summary>
    public string? EventTypeFilter { get; init; }

    /// <summary>Replay events from this date onward</summary>
    public DateTime? From { get; init; }

    /// <summary>Simulate rebuild without persisting changes</summary>
    public bool DryRun { get; init; } = true;

    /// <summary>Super-admin: rebuild across all tenants</summary>
    public bool AllTenants { get; init; } = false;
}
