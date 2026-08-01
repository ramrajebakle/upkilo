using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

using Upkilo.API.Models;

namespace Upkilo.API.Controllers;

/// <summary>
/// Custom roles builder: lets tenant admins create and manage
/// custom roles with fine-grained permissions.
/// </summary>
[ApiController]
// Was "api/v{version}/roles", which collided with RolesController's [controller] route
// (routing is case-insensitive). Every request to /api/v1/roles then failed with
// 500 "The request matched multiple endpoints", so BOTH controllers were unreachable.
// RolesController keeps /roles — it also surfaces the built-in system roles the settings
// page needs; this controller serves tenant-defined custom roles only.
[Route("api/v{version:apiVersion}/custom-roles")]
[Authorize(Roles = "Owner,Admin")]
public class CustomRolesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CustomRolesController> _logger;

    public CustomRolesController(AppDbContext context, ITenantProvider tenantProvider, ILogger<CustomRolesController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    // DTOs for role creation and updates — keep lightweight and explicit for API stability
    // Use centralized DTOs from Upkilo.API.Models (RoleDtos.cs)


    /// <summary>
    /// Get all custom roles for this tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var roles = await _context.Set<CustomRole>()
            .Where(r => r.TenantId == tenantId.Value)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Ok(roles);
    }

    /// <summary>
    /// Create a new custom role
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] Upkilo.API.Models.CreateRoleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Role name is required." });

        // Check for duplicate name (case-insensitive)
        var exists = await _context.Set<CustomRole>()
            .AnyAsync(r => r.TenantId == tenantId.Value && r.Name.ToLower() == request.Name.ToLower());
        if (exists) return Conflict(new { error = "A role with this name already exists." });

        var role = new CustomRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Permissions = request.Permissions ?? new Dictionary<string, bool>(),
            IsSystem = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<CustomRole>().Add(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Custom role '{RoleName}' created for tenant {TenantId}", role.Name, tenantId);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
    }

    /// <summary>
    /// Get a specific custom role
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value);

        if (role == null) return NotFound();
        return Ok(role);
    }

    /// <summary>
    /// Update a custom role's properties and permissions
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] Upkilo.API.Models.UpdateRoleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value);
        if (role == null) return NotFound();

        if (role.IsSystem)
            return BadRequest(new { error = "Cannot modify system roles." });

        if (!string.IsNullOrWhiteSpace(request.Name)) role.Name = request.Name;
        if (request.Description != null) role.Description = request.Description;
        if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;
        if (request.Permissions != null) role.Permissions = request.Permissions;

        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Custom role '{RoleName}' updated for tenant {TenantId}", role.Name, tenantId);

        return Ok(role);
    }

    /// <summary>
    /// Delete a custom role
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId.Value);
        if (role == null) return NotFound();

        _context.Set<CustomRole>().Remove(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Custom role '{RoleName}' deleted for tenant {TenantId}", role.Name, tenantId);
        return NoContent();
    }
}
