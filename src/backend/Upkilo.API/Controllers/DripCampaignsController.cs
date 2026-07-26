using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Multi-channel drip campaigns — automated multi-step sequences per tenant.
/// Backs the /marketing/drip-campaigns page.
/// </summary>
[ApiController]
[Route("api/v1/drip-campaigns")]
[Authorize]
public class DripCampaignsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DripCampaignsController> _logger;

    public DripCampaignsController(AppDbContext context, ITenantProvider tenantProvider, ILogger<DripCampaignsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public record DripStepDto(int stepNumber, string channel, int delayDays, int delayHours, string? subject, string body, string? condition);
    public record CreateDripCampaignRequest(string name, string? description, string triggerType, List<DripStepDto>? steps);

    private object Project(DripCampaign c)
    {
        JsonElement steps;
        try { steps = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(c.StepsJson) ? "[]" : c.StepsJson); }
        catch { steps = JsonSerializer.Deserialize<JsonElement>("[]"); }
        return new
        {
            id = c.Id,
            name = c.Name,
            description = c.Description,
            triggerType = c.TriggerType,
            status = c.Status,
            steps,
            enrolledCount = c.EnrolledCount,
            completedCount = c.CompletedCount,
            openRate = c.OpenRate,
            clickRate = c.ClickRate,
            createdAt = c.CreatedAt,
        };
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaigns = await _context.DripCampaigns
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(campaigns.Select(Project));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDripCampaignRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.name)) return BadRequest(new { message = "Campaign name is required." });

        // Re-number steps deterministically to avoid trusting client ordering.
        var steps = (request.steps ?? new List<DripStepDto>())
            .Select((s, i) => s with { stepNumber = i + 1 })
            .ToList();

        var campaign = new DripCampaign
        {
            TenantId = tenantId.Value,
            Name = request.name.Trim(),
            Description = request.description,
            TriggerType = string.IsNullOrWhiteSpace(request.triggerType) ? "signup" : request.triggerType,
            Status = "draft",
            StepsJson = JsonSerializer.Serialize(steps),
        };

        _context.DripCampaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { id = campaign.Id }, Project(campaign));
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.DripCampaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);
        if (campaign == null) return NotFound();

        campaign.Status = campaign.Status == "active" ? "paused" : "active";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(Project(campaign));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var campaign = await _context.DripCampaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value && !c.IsDeleted);
        if (campaign == null) return NotFound();

        campaign.IsDeleted = true;
        campaign.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
