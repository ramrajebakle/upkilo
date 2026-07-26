using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService, 
        ITenantProvider tenantProvider,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;

    /// <summary>
    /// Get all locations for tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var locations = await _locationService.GetAllAsync(GetTenantId());
        return Ok(locations);
    }

    /// <summary>
    /// Get a specific location
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var location = await _locationService.GetByIdAsync(id, GetTenantId());
        if (location == null)
            return NotFound(new { error = "Location not found" });

        return Ok(location);
    }

    /// <summary>
    /// Create a new location
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationRequest request)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var location = new Location
        {
            Name = request.Name,
            Description = request.Description,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            Country = request.Country,
            PostalCode = request.PostalCode,
            Phone = request.Phone,
            Email = request.Email,
            Timezone = request.Timezone ?? "UTC",
            BusinessHours = request.BusinessHours,
            Holidays = request.Holidays,
            IsActive = true
        };

        var created = await _locationService.CreateAsync(tenantId, location);
        return Ok(created);
    }

    /// <summary>
    /// Update a location
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LocationRequest request)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty) return Unauthorized();

        var updates = new Location
        {
            Name = request.Name,
            Description = request.Description,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            Country = request.Country,
            PostalCode = request.PostalCode,
            Phone = request.Phone,
            Email = request.Email,
            Timezone = request.Timezone,
            BusinessHours = request.BusinessHours,
            Holidays = request.Holidays,
            IsActive = request.IsActive ?? true
        };

        var updated = await _locationService.UpdateAsync(id, tenantId, updates);
        if (updated == null)
            return NotFound(new { error = "Location not found" });

        return Ok(updated);
    }

    /// <summary>
    /// Delete a location
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _locationService.DeleteAsync(id, GetTenantId());
        if (!success)
            return NotFound(new { error = "Location not found" });

        return Ok(new { message = "Location deleted" });
    }

    /// <summary>
    /// Set a location as primary/default
    /// </summary>
    [HttpPost("{id}/primary")]
    public async Task<IActionResult> SetPrimary(Guid id)
    {
        var success = await _locationService.SetDefaultAsync(id, GetTenantId());
        if (!success)
            return NotFound(new { error = "Location not found" });

        return Ok(new { message = "Primary location updated" });
    }
}

public record LocationRequest(
    string Name,
    string? Description,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    string? Phone,
    string? Email,
    string? Timezone,
    string? BusinessHours,
    string? Holidays,
    bool? IsActive
);

