using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Contraindications controller for managing client health alerts
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ContraindicationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ContraindicationsController> _logger;

    public ContraindicationsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<ContraindicationsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all contraindications for a client
    /// </summary>
    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetClientContraindications(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var contraindications = await _context.Set<ClientContraindication>()
            .Where(c => c.ClientId == clientId && c.TenantId == tenantId && c.IsActive)
            .OrderByDescending(c => c.Severity)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        var hasCritical = contraindications.Any(c => c.Severity == ContraindicationSeverity.Critical);
        var hasHigh = contraindications.Any(c => c.Severity == ContraindicationSeverity.High);

        return Ok(new
        {
            data = contraindications,
            summary = new
            {
                total = contraindications.Count,
                hasCritical,
                hasHigh,
                alertLevel = hasCritical ? "critical" : hasHigh ? "high" : contraindications.Any() ? "info" : "none"
            }
        });
    }

    /// <summary>
    /// Add a contraindication for a client
    /// </summary>
    [HttpPost("client/{clientId}")]
    public async Task<IActionResult> AddContraindication(Guid clientId, [FromBody] AddContraindicationRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        var contraindication = new ClientContraindication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = clientId,
            Type = request.Type,
            Title = request.Title,
            Description = request.Description,
            Severity = request.Severity,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<ClientContraindication>().Add(contraindication);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added contraindication {Type} for client {ClientId}", request.Type, clientId);

        return CreatedAtAction(nameof(GetClientContraindications), new { clientId }, contraindication);
    }

    /// <summary>
    /// Update a contraindication
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContraindication(Guid id, [FromBody] UpdateContraindicationRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var contraindication = await _context.Set<ClientContraindication>()
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (contraindication == null) return NotFound();

        if (request.Title != null) contraindication.Title = request.Title;
        if (request.Description != null) contraindication.Description = request.Description;
        if (request.Severity.HasValue) contraindication.Severity = request.Severity.Value;
        if (request.IsActive.HasValue) contraindication.IsActive = request.IsActive.Value;
        if (request.ExpiresAt.HasValue) contraindication.ExpiresAt = request.ExpiresAt.Value;

        contraindication.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(contraindication);
    }

    /// <summary>
    /// Remove a contraindication
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContraindication(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var contraindication = await _context.Set<ClientContraindication>()
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

        if (contraindication == null) return NotFound();

        // Soft delete
        contraindication.IsActive = false;
        contraindication.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Check contraindications before booking (returns warnings/blocks)
    /// </summary>
    [HttpGet("check/{clientId}/for-booking")]
    public async Task<IActionResult> CheckForBooking(Guid clientId, [FromQuery] Guid? serviceId = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var now = DateTime.UtcNow;
        var activeContraindications = await _context.Set<ClientContraindication>()
            .Where(c => c.ClientId == clientId &&
                        c.TenantId == tenantId &&
                        c.IsActive &&
                        (c.ExpiresAt == null || c.ExpiresAt > now))
            .ToListAsync();

        var criticalAlerts = activeContraindications.Where(c => c.Severity == ContraindicationSeverity.Critical).ToList();
        var warnings = activeContraindications.Where(c => c.Severity != ContraindicationSeverity.Critical).ToList();

        return Ok(new
        {
            canProceed = !criticalAlerts.Any(),
            criticalAlerts = criticalAlerts.Select(c => new { c.Id, c.Type, c.Title, c.Description }),
            warnings = warnings.Select(c => new { c.Id, c.Type, c.Title, c.Severity }),
            message = criticalAlerts.Any()
                ? "Critical contraindication(s) detected. Service may need to be declined."
                : warnings.Any()
                    ? $"{warnings.Count} contraindication(s) to be aware of"
                    : "No contraindications on file"
        });
    }

    /// <summary>
    /// Get clients with active contraindications
    /// </summary>
    [HttpGet("clients-with-alerts")]
    public async Task<IActionResult> GetClientsWithAlerts()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var clientsWithAlerts = await _context.Set<ClientContraindication>()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .GroupBy(c => c.ClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                AlertCount = g.Count(),
                MaxSeverity = g.Max(c => c.Severity)
            })
            .OrderByDescending(x => x.MaxSeverity)
            .ThenByDescending(x => x.AlertCount)
            .Take(50)
            .ToListAsync();

        // Get client details
        var clientIds = clientsWithAlerts.Select(x => x.ClientId).ToList();
        var clients = await _context.Clients
            .Where(c => clientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c);

        var result = clientsWithAlerts.Select(x => new
        {
            x.ClientId,
            ClientName = clients.TryGetValue(x.ClientId, out var c) ? c.FullName : "Unknown",
            x.AlertCount,
            x.MaxSeverity
        }).ToList();

        return Ok(new { data = result });
    }
}

// DTOs
public record AddContraindicationRequest(
    ContraindicationType Type,
    string Title,
    string? Description = null,
    ContraindicationSeverity Severity = ContraindicationSeverity.Moderate,
    DateTime? ExpiresAt = null
);

public record UpdateContraindicationRequest(
    string? Title = null,
    string? Description = null,
    ContraindicationSeverity? Severity = null,
    bool? IsActive = null,
    DateTime? ExpiresAt = null
);

