using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Equipment controller for managing business assets
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EquipmentController> _logger;

    public EquipmentController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<EquipmentController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all equipment
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEquipment([FromQuery] string? category = null, [FromQuery] EquipmentStatus? status = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<Equipment>()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var equipment = await query
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Category,
                e.Status,
                e.Location,
                e.NextMaintenanceDate,
                MaintenanceDue = e.NextMaintenanceDate.HasValue && e.NextMaintenanceDate <= DateTime.UtcNow.AddDays(7)
            })
            .ToListAsync();

        return Ok(new { data = equipment });
    }

    /// <summary>
    /// Get equipment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEquipmentById(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var equipment = await _context.Set<Equipment>()
            .Include(e => e.AssignedToStaff)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (equipment == null) return NotFound();

        return Ok(equipment);
    }

    /// <summary>
    /// Create equipment
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEquipment([FromBody] CreateEquipmentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            SerialNumber = request.SerialNumber,
            Model = request.Model,
            Manufacturer = request.Manufacturer,
            PurchaseDate = request.PurchaseDate,
            PurchasePrice = request.PurchasePrice,
            WarrantyExpiry = request.WarrantyExpiry,
            Location = request.Location,
            AssignedToStaffId = request.AssignedToStaffId,
            Status = EquipmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Equipment>().Add(equipment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created equipment {Name}", request.Name);

        return CreatedAtAction(nameof(GetEquipmentById), new { id = equipment.Id }, equipment);
    }

    /// <summary>
    /// Update equipment
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEquipment(Guid id, [FromBody] UpdateEquipmentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var equipment = await _context.Set<Equipment>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (equipment == null) return NotFound();

        if (request.Name != null) equipment.Name = request.Name;
        if (request.Description != null) equipment.Description = request.Description;
        if (request.Category != null) equipment.Category = request.Category;
        if (request.Location != null) equipment.Location = request.Location;
        if (request.Status.HasValue) equipment.Status = request.Status.Value;
        if (request.AssignedToStaffId.HasValue) equipment.AssignedToStaffId = request.AssignedToStaffId;
        if (request.NextMaintenanceDate.HasValue) equipment.NextMaintenanceDate = request.NextMaintenanceDate;
        if (request.Notes != null) equipment.Notes = request.Notes;

        equipment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(equipment);
    }

    /// <summary>
    /// Add maintenance record
    /// </summary>
    [HttpPost("{id}/maintenance")]
    public async Task<IActionResult> AddMaintenance(Guid id, [FromBody] AddMaintenanceRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var equipment = await _context.Set<Equipment>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (equipment == null) return NotFound();

        var maintenance = new EquipmentMaintenance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            EquipmentId = id,
            Type = request.Type,
            PerformedAt = request.PerformedAt ?? DateTime.UtcNow,
            Description = request.Description,
            Cost = request.Cost,
            PerformedBy = request.PerformedBy,
            NextDueDate = request.NextDueDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update equipment
        equipment.LastMaintenanceDate = maintenance.PerformedAt;
        if (request.NextDueDate.HasValue)
            equipment.NextMaintenanceDate = request.NextDueDate;

        _context.Set<EquipmentMaintenance>().Add(maintenance);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added maintenance record for equipment {EquipmentId}", id);

        return Ok(maintenance);
    }

    /// <summary>
    /// Get maintenance history
    /// </summary>
    [HttpGet("{id}/maintenance")]
    public async Task<IActionResult> GetMaintenance(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var records = await _context.Set<EquipmentMaintenance>()
            .Where(m => m.EquipmentId == id && m.TenantId == tenantId)
            .OrderByDescending(m => m.PerformedAt)
            .ToListAsync();

        return Ok(new { data = records });
    }

    /// <summary>
    /// Get maintenance due soon
    /// </summary>
    [HttpGet("maintenance-due")]
    public async Task<IActionResult> GetMaintenanceDue([FromQuery] int daysAhead = 14)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var cutoff = DateTime.UtcNow.AddDays(daysAhead);

        var due = await _context.Set<Equipment>()
            .Where(e => e.TenantId == tenantId &&
                        e.Status == EquipmentStatus.Active &&
                        e.NextMaintenanceDate.HasValue &&
                        e.NextMaintenanceDate <= cutoff)
            .OrderBy(e => e.NextMaintenanceDate)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Category,
                e.NextMaintenanceDate,
                DaysUntilDue = e.NextMaintenanceDate.HasValue ? (e.NextMaintenanceDate.Value - DateTime.UtcNow).Days : (int?)null,
                IsOverdue = e.NextMaintenanceDate < DateTime.UtcNow
            })
            .ToListAsync();

        return Ok(new
        {
            data = due,
            summary = new
            {
                overdue = due.Count(x => x.IsOverdue),
                upcoming = due.Count(x => !x.IsOverdue)
            }
        });
    }

    /// <summary>
    /// Get equipment value summary
    /// </summary>
    [HttpGet("value")]
    public async Task<IActionResult> GetEquipmentValue()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var equipment = await _context.Set<Equipment>()
            .Where(e => e.TenantId == tenantId && e.Status != EquipmentStatus.Retired)
            .ToListAsync();

        var totalValue = equipment.Where(e => e.PurchasePrice.HasValue).Sum(e => e.PurchasePrice!.Value);
        var byCategory = equipment.GroupBy(e => e.Category ?? "Uncategorized")
            .Select(g => new { Category = g.Key, Count = g.Count(), Value = g.Where(e => e.PurchasePrice.HasValue).Sum(e => e.PurchasePrice!.Value) })
            .ToList();

        return Ok(new
        {
            totalItems = equipment.Count,
            totalValue,
            byCategory,
            byStatus = equipment.GroupBy(e => e.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToList()
        });
    }
}

// DTOs
public record CreateEquipmentRequest(
    string Name,
    string? Description = null,
    string? Category = null,
    string? SerialNumber = null,
    string? Model = null,
    string? Manufacturer = null,
    DateTime? PurchaseDate = null,
    decimal? PurchasePrice = null,
    DateTime? WarrantyExpiry = null,
    string? Location = null,
    Guid? AssignedToStaffId = null
);

public record UpdateEquipmentRequest(
    string? Name = null,
    string? Description = null,
    string? Category = null,
    string? Location = null,
    EquipmentStatus? Status = null,
    Guid? AssignedToStaffId = null,
    DateTime? NextMaintenanceDate = null,
    string? Notes = null
);

public record AddMaintenanceRequest(
    MaintenanceType Type,
    DateTime? PerformedAt = null,
    string? Description = null,
    decimal? Cost = null,
    string? PerformedBy = null,
    DateTime? NextDueDate = null,
    string? Notes = null
);

