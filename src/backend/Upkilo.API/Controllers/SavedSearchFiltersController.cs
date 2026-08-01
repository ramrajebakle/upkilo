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
public class SavedSearchFiltersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SavedSearchFiltersController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetFilters([FromQuery] string targetEntity)
    {
        var tenantId = _tenantProvider.GetTenantId();

        // Get user ID from authentication context
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var filters = await _context.SavedSearchFilters
            .Where(f => f.TenantId == tenantId && f.UserId == userId && f.TargetEntity == targetEntity)
            .OrderBy(f => f.Name)
            .ToListAsync();

        return Ok(filters);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFilter([FromBody] CreateSavedFilterRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var filter = new SavedSearchFilter
        {
            TenantId = tenantId.Value,
            UserId = request.UserId, // From Auth Context implicitly normally
            Name = request.Name,
            TargetEntity = request.TargetEntity,
            FilterJson = request.FilterJson,
            IsDefault = request.IsDefault
        };

        if (filter.IsDefault)
        {
            // Reset other default filters for this entity and user
            var existingDefault = await _context.SavedSearchFilters
                .FirstOrDefaultAsync(f => f.UserId == filter.UserId && f.TargetEntity == filter.TargetEntity && f.IsDefault);

            if (existingDefault != null) existingDefault.IsDefault = false;
        }

        _context.SavedSearchFilters.Add(filter);
        await _context.SaveChangesAsync();

        return Ok(filter);
    }
}

public class CreateSavedFilterRequest
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string FilterJson { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
