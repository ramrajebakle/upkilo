using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Filters;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ReadReplicaFilter] // SC1: route to read replica
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IReportingService _reportingService;

    public ReportsController(AppDbContext context, IReportingService reportingService)
    {
        _context = context;
        _reportingService = reportingService;
    }

    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions()
    {
        var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
        var definitions = await _context.Set<ReportDefinition>()
            .Where(r => r.TenantId == tenantId && !r.IsArchived)
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new { r.Id, r.Name, r.Description, r.ReportType, r.IsScheduled, r.CreatedAt, r.LastRunAt })
            .ToListAsync();
        return Ok(definitions);
    }

    // VULN-A10 FIX: replaced [FromBody] ReportDefinition (full entity) with a narrow DTO.
    // The entity form allowed callers to force-set Id, TenantId, IsArchived, IsPublic, etc.
    [HttpPost("definitions")]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateReportDefinitionRequest request)
    {
        var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
        var definition = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            ConfigJson = request.ConfigJson ?? "{}",
            ReportType = request.ReportType ?? "Custom",
            IsArchived = false,
            IsPublic = false
        };
        _context.Set<ReportDefinition>().Add(definition);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDefinitions), new { id = definition.Id }, definition);
    }

    [HttpGet("execute/{id}")]
    public async Task<IActionResult> Execute(Guid id, [FromQuery] string format = "json")
    {
        var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
        var definition = await _context.Set<ReportDefinition>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (definition == null) return NotFound();

        var result = await _reportingService.ExecuteReportAsync(tenantId, definition);
        
        if (format.ToLower() == "csv")
        {
            // CSV serialization logic
            return Ok(result.Rows);
        }

        return Ok(result);
    }

    [HttpGet("funnel")]
    public async Task<IActionResult> GetFunnel([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var tenantId = Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var funnel = await _reportingService.GetFunnelAnalyticsAsync(tenantId, startDate, endDate);
        return Ok(funnel);
    }
}

/// <summary>VULN-A10: DTO prevents mass assignment on the ReportDefinition entity.</summary>
public record CreateReportDefinitionRequest(
    string Name,
    string? Description,
    string? ConfigJson,
    string? ReportType
);
