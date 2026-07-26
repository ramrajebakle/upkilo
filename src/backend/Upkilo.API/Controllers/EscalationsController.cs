using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class EscalationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EscalationsController> _logger;

    public EscalationsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<EscalationsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() 
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// Get all unresolved system escalations for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEscalations([FromQuery] string? module = null, [FromQuery] string? severity = null)
    {
        var query = _context.Set<AIEscalation>()
            .Where(e => e.TenantId == GetTenantId() && !e.IsResolved);

        if (!string.IsNullOrEmpty(module))
            query = query.Where(e => e.Module == module);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(e => e.Severity == severity);

        var items = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        return Ok(new
        {
            items,
            total = items.Count,
            pending = items.Count(i => !i.IsResolved),
            modules = items.GroupBy(i => i.Module).ToDictionary(g => g.Key, g => g.Count())
        });
    }

    /// <summary>
    /// Resolve or Approve a system escalation
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveEscalation(Guid id, [FromBody] EscalationResolutionRequest request)
    {
        var escalation = await _context.Set<AIEscalation>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == GetTenantId());

        if (escalation == null) return NotFound();

        escalation.IsResolved = true;
        escalation.ResolvedAt = DateTime.UtcNow;
        escalation.ResolvedBy = _tenantProvider.GetUserId()?.ToString() ?? "System";
        escalation.ResolutionNotes = request.Notes;
        escalation.IsApproved = request.Approved;
        escalation.ActionTaken = request.Approved ? "Approved" : "Rejected";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Escalation {Id} ({Module}) resolved as {Action} by user {UserId}", 
            id, escalation.Module, escalation.ActionTaken, escalation.ResolvedBy);

        return Ok(new { message = "Escalation resolved", item = escalation });
    }

    /// <summary>
    /// Get escalation statistics for the tenant
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _context.Set<AIEscalation>()
            .Where(e => e.TenantId == GetTenantId())
            .GroupBy(e => new { e.Module, e.IsResolved })
            .Select(g => new { 
                Module = g.Key.Module, 
                IsResolved = g.Key.IsResolved, 
                Count = g.Count() 
            })
            .ToListAsync();

        return Ok(stats);
    }
}

public record EscalationResolutionRequest(bool Approved, string? Notes);
