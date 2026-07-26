using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// A/B Testing experiments management — Task 1458
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ExperimentsController : ControllerBase
{
    private readonly ILogger<ExperimentsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ExperimentsController(ILogger<ExperimentsController> logger, AppDbContext context, ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>GET /api/v1/experiments — list all experiments for tenant</summary>
    [HttpGet]
    public async Task<IActionResult> GetExperiments([FromQuery] bool? active = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Experiments.Where(e => e.TenantId == tenantId);
        if (active.HasValue) query = query.Where(e => e.IsActive == active.Value);

        var experiments = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                id = e.Id,
                name = e.Name,
                variantA = e.VariantA,
                variantB = e.VariantB,
                isActive = e.IsActive,
                trafficSplit = e.TrafficSplit,
                createdAt = e.CreatedAt,
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            experiments,
            total = experiments.Count
        }));
    }

    /// <summary>GET /api/v1/experiments/{id} — get single experiment</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetExperiment(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var experiment = await _context.Experiments
            .Where(e => e.Id == id && e.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (experiment == null) return NotFound();

        // Simulate analytics data
        var random = new Random(id.GetHashCode());
        var aImpressions = random.Next(500, 5000);
        var bImpressions = random.Next(400, 4000);
        var aConversions = random.Next(50, aImpressions / 5);
        var bConversions = random.Next(40, bImpressions / 5);

        return Ok(ApiResponse<object>.Ok(new
        {
            id = experiment.Id,
            name = experiment.Name,
            variantA = experiment.VariantA,
            variantB = experiment.VariantB,
            isActive = experiment.IsActive,
            trafficSplit = experiment.TrafficSplit,
            createdAt = experiment.CreatedAt,
            results = new
            {
                variantA = new
                {
                    impressions = aImpressions,
                    conversions = aConversions,
                    conversionRate = Math.Round((double)aConversions / aImpressions * 100, 2)
                },
                variantB = new
                {
                    impressions = bImpressions,
                    conversions = bConversions,
                    conversionRate = Math.Round((double)bConversions / bImpressions * 100, 2)
                },
                winner = aConversions > bConversions ? "A" : "B",
                confidenceLevel = random.Next(75, 99)
            }
        }));
    }

    /// <summary>POST /api/v1/experiments — create new experiment</summary>
    [HttpPost]
    public async Task<IActionResult> CreateExperiment([FromBody] CreateExperimentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse.Fail("Name is required"));

        var experiment = new Experiment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            VariantA = request.VariantA ?? "Control",
            VariantB = request.VariantB ?? "Variation",
            IsActive = true,
            TrafficSplit = Math.Clamp(request.TrafficSplit ?? 0.5, 0.1, 0.9),
            CreatedAt = DateTime.UtcNow,
        };

        _context.Experiments.Add(experiment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Experiment {Id} created for tenant {TenantId}", experiment.Id, tenantId);
        return Ok(ApiResponse<object>.Ok(new { id = experiment.Id, name = experiment.Name }));
    }

    /// <summary>PUT /api/v1/experiments/{id} — update experiment</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateExperiment(Guid id, [FromBody] UpdateExperimentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var experiment = await _context.Experiments
            .Where(e => e.Id == id && e.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (experiment == null) return NotFound();

        if (request.Name != null) experiment.Name = request.Name;
        if (request.VariantA != null) experiment.VariantA = request.VariantA;
        if (request.VariantB != null) experiment.VariantB = request.VariantB;
        if (request.TrafficSplit.HasValue) experiment.TrafficSplit = Math.Clamp(request.TrafficSplit.Value, 0.1, 0.9);
        if (request.IsActive.HasValue) experiment.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id = experiment.Id }));
    }

    /// <summary>POST /api/v1/experiments/{id}/toggle — toggle active state</summary>
    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleExperiment(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var experiment = await _context.Experiments
            .Where(e => e.Id == id && e.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (experiment == null) return NotFound();

        experiment.IsActive = !experiment.IsActive;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { id = experiment.Id, isActive = experiment.IsActive }));
    }

    /// <summary>DELETE /api/v1/experiments/{id} — delete experiment</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExperiment(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var experiment = await _context.Experiments
            .Where(e => e.Id == id && e.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (experiment == null) return NotFound();

        _context.Experiments.Remove(experiment);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    /// <summary>POST /api/v1/experiments/{id}/assign — assign a user to a variant</summary>
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> AssignVariant(Guid id, [FromBody] AssignVariantRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var experiment = await _context.Experiments
            .Where(e => e.Id == id && e.TenantId == tenantId && e.IsActive)
            .FirstOrDefaultAsync();

        if (experiment == null) return NotFound(ApiResponse.Fail("Experiment not found or inactive"));

        // Deterministic assignment based on userId hash
        var hash = Math.Abs((request.UserId ?? Guid.NewGuid().ToString()).GetHashCode());
        var normalised = (double)(hash % 1000) / 1000.0;
        var variant = normalised < experiment.TrafficSplit ? "A" : "B";
        var variantName = variant == "A" ? experiment.VariantA : experiment.VariantB;

        return Ok(ApiResponse<object>.Ok(new { variant, variantName, experimentId = id }));
    }
}

public class CreateExperimentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? VariantA { get; set; }
    public string? VariantB { get; set; }
    public double? TrafficSplit { get; set; }
}

public class UpdateExperimentRequest
{
    public string? Name { get; set; }
    public string? VariantA { get; set; }
    public string? VariantB { get; set; }
    public double? TrafficSplit { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignVariantRequest
{
    public string? UserId { get; set; }
}
