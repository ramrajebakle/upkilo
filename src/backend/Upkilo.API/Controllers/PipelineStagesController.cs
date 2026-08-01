using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PipelineStagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public PipelineStagesController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetStages([FromQuery] Guid pipelineId)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var stages = await _context.PipelineStages
            .Where(s => s.PipelineId == pipelineId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();

        return Ok(stages);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStage([FromBody] CreatePipelineStageRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var stage = new PipelineStage
        {
            TenantId = tenantId.Value,
            PipelineId = request.PipelineId,
            Name = request.Name,
            OrderIndex = request.OrderIndex,
            ProbabilityPercentage = request.ProbabilityPercentage
        };

        _context.PipelineStages.Add(stage);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStages), new { pipelineId = stage.PipelineId }, stage);
    }
}

public class CreatePipelineStageRequest
{
    public Guid PipelineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int ProbabilityPercentage { get; set; }
}
