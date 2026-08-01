using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Packages controller for service bundle management.
/// Uses real database queries against ServicePackages.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PackagesController : ControllerBase
{
    private readonly ILogger<PackagesController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public PackagesController(
        ILogger<PackagesController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all packages
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPackages([FromQuery] bool? isActive = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.ServicePackages
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var packages = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                originalPrice = p.OriginalPrice,
                price = p.TotalPrice,
                savings = p.OriginalPrice.HasValue ? p.OriginalPrice.Value - p.TotalPrice : 0,
                serviceIds = p.ServiceIds,
                sessionCount = p.SessionCount,
                sessionsUsed = p.SessionsUsed,
                sessionsRemaining = p.SessionCount - p.SessionsUsed,
                p.ValidityDays,
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = packages });
    }

    /// <summary>
    /// Get package by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPackage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (package == null) return NotFound();

        return Ok(new
        {
            package.Id,
            package.Name,
            package.Description,
            originalPrice = package.OriginalPrice,
            price = package.TotalPrice,
            savings = package.OriginalPrice.HasValue ? package.OriginalPrice.Value - package.TotalPrice : 0,
            serviceIds = package.ServiceIds,
            sessionCount = package.SessionCount,
            sessionsUsed = package.SessionsUsed,
            sessionsRemaining = package.SessionCount - package.SessionsUsed,
            package.ValidityDays,
            package.IsActive,
            package.CreatedAt,
            package.UpdatedAt
        });
    }

    /// <summary>
    /// Create a package
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePackage([FromBody] CreatePackageRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Package name is required." });

        var serviceIdsJson = request.Services != null && request.Services.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(request.Services.Select(s => new { s.ServiceId, s.Quantity }))
            : "[]";

        var totalSessions = request.Services?.Sum(s => s.Quantity) ?? 0;

        var package = new ServicePackage
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            TotalPrice = request.Price,
            OriginalPrice = request.OriginalPrice,
            ServiceIds = serviceIdsJson,
            SessionCount = totalSessions,
            ValidityDays = request.ValidityDays,
            IsActive = true
        };

        _context.ServicePackages.Add(package);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Package created: {Id} - {Name}", package.Id, package.Name);

        return CreatedAtAction(nameof(GetPackage), new { id = package.Id }, new
        {
            package.Id,
            package.Name,
            package.TotalPrice,
            isActive = true,
            package.CreatedAt
        });
    }

    /// <summary>
    /// Update a package
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] UpdatePackageRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (package == null) return NotFound();

        if (request.Name != null) package.Name = request.Name;
        if (request.Description != null) package.Description = request.Description;
        if (request.Price.HasValue) package.TotalPrice = request.Price.Value;
        if (request.ValidityDays.HasValue) package.ValidityDays = request.ValidityDays.Value;
        if (request.IsActive.HasValue) package.IsActive = request.IsActive.Value;
        package.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Package updated: {PackageId}", id);
        return Ok(new { success = true, package.UpdatedAt });
    }

    /// <summary>
    /// Delete a package (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePackage(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (package == null) return NotFound();

        package.IsDeleted = true;
        package.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Package deleted: {PackageId}", id);
        return NoContent();
    }

    /// <summary>
    /// Redeem a service session from a package
    /// </summary>
    [HttpPost("{id}/redeem")]
    public async Task<IActionResult> RedeemService(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var package = await _context.ServicePackages
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);

            if (package == null) return NotFound();

            if (!package.IsActive)
                return BadRequest(new { error = "This package is not currently active." });

            if (package.SessionsUsed >= package.SessionCount)
                return BadRequest(new { error = $"All sessions have been used ({package.SessionsUsed}/{package.SessionCount})." });

            package.SessionsUsed++;
            package.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Package session redeemed: {PackageId}, used {Used}/{Total} in tenant {TenantId}",
                id, package.SessionsUsed, package.SessionCount, tenantId);

            return Ok(new
            {
                success = true,
                sessionsUsed = package.SessionsUsed,
                sessionsRemaining = package.SessionCount - package.SessionsUsed,
                isComplete = package.SessionsUsed >= package.SessionCount,
                redeemedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to redeem package session for package {Id}", id);
            return StatusCode(500, "Internal server error occurred while redeeming session.");
        }
    }

    /// <summary>
    /// Get package analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var packages = await _context.ServicePackages
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted)
            .ToListAsync();

        var totalPackages = packages.Count;
        var totalRevenue = packages.Sum(p => p.TotalPrice);
        var totalSessions = packages.Sum(p => p.SessionCount);
        var totalUsed = packages.Sum(p => p.SessionsUsed);

        var topPackages = packages
            .OrderByDescending(p => p.SessionsUsed)
            .Take(5)
            .Select(p => new { p.Name, p.SessionCount, p.SessionsUsed, revenue = p.TotalPrice })
            .ToList();

        return Ok(new
        {
            totalPackages,
            totalRevenue,
            averagePrice = totalPackages > 0 ? Math.Round(totalRevenue / totalPackages, 2) : 0,
            topPackages,
            redemptionRate = totalSessions > 0 ? Math.Round((double)totalUsed / totalSessions * 100, 1) : 0
        });
    }
}

// Request DTOs
public class CreatePackageRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public List<PackageServiceItem> Services { get; set; } = new();
    public int ValidityDays { get; set; } = 90;
    public string? TermsAndConditions { get; set; }
}

public class PackageServiceItem
{
    public Guid ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdatePackageRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? ValidityDays { get; set; }
    public bool? IsActive { get; set; }
}

