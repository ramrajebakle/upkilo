using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Models;

namespace Upkilo.API.Controllers;

/// <summary>
/// Roles controller for managing custom roles
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<RolesController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles (system + custom)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] bool? isActive = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // System roles (from UserRole enum)
        var systemRoles = new[]
        {
            new { Id = Guid.Empty, Name = "Owner", IsSystem = true, Description = "Full system access", UserCount = 0 },
            new { Id = Guid.Empty, Name = "Admin", IsSystem = true, Description = "Administrative access", UserCount = 0 },
            new { Id = Guid.Empty, Name = "Manager", IsSystem = true, Description = "Management access", UserCount = 0 },
            new { Id = Guid.Empty, Name = "Staff", IsSystem = true, Description = "Staff access", UserCount = 0 }
        };

        // Custom roles
        var query = _context.Set<CustomRole>()
            .Where(r => r.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var customRoles = await query
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsSystem,
                UserCount = r.Users.Count,
                r.IsActive,
                r.CreatedAt
            })
            .ToListAsync();

        var allRoles = systemRoles.Concat(customRoles.Select(r => new
        {
            r.Id,
            r.Name,
            r.IsSystem,
            r.Description,
            r.UserCount
        }));

        return Ok(new { data = allRoles });
    }

    /// <summary>
    /// Get role details
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (role == null) return NotFound();

        return Ok(role);
    }

    /// <summary>
    /// Create custom role
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Check for duplicate name
        var exists = await _context.Set<CustomRole>()
            .AnyAsync(r => r.TenantId == tenantId && r.Name.ToLower() == request.Name.ToLower());

        if (exists)
            return BadRequest("A role with this name already exists");

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

        _logger.LogInformation("Created custom role {RoleName} with {PermissionCount} permissions",
            request.Name, request.Permissions?.Count ?? 0);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] Models.UpdateRoleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (role == null) return NotFound();

        if (role.IsSystem)
            return BadRequest("Cannot modify system roles");

        if (!string.IsNullOrWhiteSpace(request.Name)) role.Name = request.Name!;
        if (request.Description != null) role.Description = request.Description;
        if (request.Permissions != null) role.Permissions = request.Permissions;
        if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;

        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(role);
    }

    /// <summary>
    /// Delete custom role
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (role == null) return NotFound();

        if (role.IsSystem)
            return BadRequest("Cannot delete system roles");

        if (role.Users.Any())
            return BadRequest($"Cannot delete role. {role.Users.Count} user(s) are assigned to this role");

        _context.Set<CustomRole>().Remove(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted custom role {RoleName}", role.Name);

        return NoContent();
    }

    /// <summary>
    /// Get available permissions
    /// </summary>
    [HttpGet("permissions")]
    public IActionResult GetPermissions()
    {
        var permissions = new Dictionary<string, PermissionCategory>
        {
            ["clients"] = new PermissionCategory
            {
                Label = "Clients",
                Permissions = new Dictionary<string, string>
                {
                    ["clients.view"] = "View clients",
                    ["clients.create"] = "Create clients",
                    ["clients.edit"] = "Edit clients",
                    ["clients.delete"] = "Delete clients",
                    ["clients.export"] = "Export client data"
                }
            },
            ["bookings"] = new PermissionCategory
            {
                Label = "Bookings",
                Permissions = new Dictionary<string, string>
                {
                    ["bookings.view"] = "View bookings",
                    ["bookings.create"] = "Create bookings",
                    ["bookings.edit"] = "Edit bookings",
                    ["bookings.delete"] = "Delete bookings",
                    ["bookings.cancel"] = "Cancel bookings"
                }
            },
            ["services"] = new PermissionCategory
            {
                Label = "Services",
                Permissions = new Dictionary<string, string>
                {
                    ["services.view"] = "View services",
                    ["services.create"] = "Create services",
                    ["services.edit"] = "Edit services",
                    ["services.delete"] = "Delete services"
                }
            },
            ["staff"] = new PermissionCategory
            {
                Label = "Staff",
                Permissions = new Dictionary<string, string>
                {
                    ["staff.view"] = "View staff",
                    ["staff.create"] = "Create staff",
                    ["staff.edit"] = "Edit staff",
                    ["staff.delete"] = "Delete staff"
                }
            },
            ["payments"] = new PermissionCategory
            {
                Label = "Payments",
                Permissions = new Dictionary<string, string>
                {
                    ["payments.view"] = "View payments",
                    ["payments.process"] = "Process payments",
                    ["payments.refund"] = "Refund payments"
                }
            },
            ["reports"] = new PermissionCategory
            {
                Label = "Reports",
                Permissions = new Dictionary<string, string>
                {
                    ["reports.view"] = "View reports",
                    ["reports.financial"] = "View financial reports",
                    ["reports.export"] = "Export reports"
                }
            },
            ["settings"] = new PermissionCategory
            {
                Label = "Settings",
                Permissions = new Dictionary<string, string>
                {
                    ["settings.view"] = "View settings",
                    ["settings.manage"] = "Manage settings",
                    ["settings.integrations"] = "Manage integrations"
                }
            },
            ["marketing"] = new PermissionCategory
            {
                Label = "Marketing",
                Permissions = new Dictionary<string, string>
                {
                    ["marketing.campaigns"] = "Manage campaigns",
                    ["marketing.analytics"] = "View analytics"
                }
            }
        };

        return Ok(permissions);
    }

    /// <summary>
    /// Assign role to users
    /// </summary>
    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var role = await _context.Set<CustomRole>()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (role == null) return NotFound("Role not found");

        var users = await _context.Set<User>()
            .Where(u => request.UserIds.Contains(u.Id) && u.TenantId == tenantId)
            .ToListAsync();

        if (users.Count != request.UserIds.Count)
            return BadRequest("One or more users not found");

        foreach (var user in users)
        {
            user.CustomRoleId = id;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned role {RoleName} to {UserCount} users", role.Name, users.Count);

        return Ok(new { message = $"Role assigned to {users.Count} user(s)" });
    }
}

