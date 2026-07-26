using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

/// <summary>
/// Class packages — sell bundles of class credits to clients
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ClassPackagesController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ClassPackagesController> _logger;

    // In-memory store — keyed by "tenantId:packageId"
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ClassPackage> _packages = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ClientPackage> _clientPackages = new();

    public ClassPackagesController(ITenantProvider tenantProvider, ILogger<ClassPackagesController> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>GET /api/v1/classpackages — list all packages</summary>
    [HttpGet]
    public IActionResult GetPackages()
    {
        var tenantId = GetTenantId().ToString();
        var packages = _packages.Values
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        return Ok(ApiResponse<object>.Ok(new { packages, total = packages.Count }));
    }

    /// <summary>POST /api/v1/classpackages — create a new package</summary>
    [HttpPost]
    public IActionResult CreatePackage([FromBody] CreateClassPackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(ApiResponse.Fail("Package name required"));
        if (request.Credits < 1) return BadRequest(ApiResponse.Fail("Must include at least 1 credit"));
        if (request.Price < 0) return BadRequest(ApiResponse.Fail("Price cannot be negative"));

        var tenantId = GetTenantId().ToString();
        var pkg = new ClassPackage
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Credits = request.Credits,
            Price = request.Price,
            ValidityDays = request.ValidityDays ?? 365,
            IsActive = true,
            IsTransferable = request.IsTransferable,
            ApplicableClassIds = request.ApplicableClassIds ?? Array.Empty<string>(),
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };

        _packages[$"{tenantId}:{pkg.Id}"] = pkg;
        _logger.LogInformation("Package {Name} created for tenant {TenantId}", pkg.Name, tenantId);

        return Ok(ApiResponse<object>.Ok(pkg));
    }

    /// <summary>PUT /api/v1/classpackages/{id} — update a package</summary>
    [HttpPut("{id}")]
    public IActionResult UpdatePackage(string id, [FromBody] CreateClassPackageRequest request)
    {
        var tenantId = GetTenantId().ToString();
        if (!_packages.TryGetValue($"{tenantId}:{id}", out var pkg))
            return NotFound(ApiResponse.Fail("Package not found"));

        pkg.Name = request.Name ?? pkg.Name;
        pkg.Description = request.Description ?? pkg.Description;
        pkg.Credits = request.Credits > 0 ? request.Credits : pkg.Credits;
        pkg.Price = request.Price >= 0 ? request.Price : pkg.Price;
        pkg.ValidityDays = request.ValidityDays ?? pkg.ValidityDays;
        pkg.IsTransferable = request.IsTransferable;

        return Ok(ApiResponse<object>.Ok(pkg));
    }

    /// <summary>DELETE /api/v1/classpackages/{id} — deactivate a package</summary>
    [HttpDelete("{id}")]
    public IActionResult DeletePackage(string id)
    {
        var tenantId = GetTenantId().ToString();
        if (!_packages.TryGetValue($"{tenantId}:{id}", out var pkg))
            return NotFound(ApiResponse.Fail("Package not found"));

        pkg.IsActive = false;
        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    /// <summary>POST /api/v1/classpackages/{id}/purchase — purchase package for a client</summary>
    [HttpPost("{id}/purchase")]
    public IActionResult PurchasePackage(string id, [FromBody] PurchasePackageRequest request)
    {
        var tenantId = GetTenantId().ToString();
        if (!_packages.TryGetValue($"{tenantId}:{id}", out var pkg))
            return NotFound(ApiResponse.Fail("Package not found"));

        if (!pkg.IsActive) return BadRequest(ApiResponse.Fail("Package is no longer active"));

        var clientPkg = new ClientPackage
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            PackageId = id,
            PackageName = pkg.Name,
            ClientId = request.ClientId,
            TotalCredits = pkg.Credits,
            UsedCredits = 0,
            RemainingCredits = pkg.Credits,
            PurchasePrice = pkg.Price,
            PurchasedAt = DateTime.UtcNow.ToString("o"),
            ExpiresAt = DateTime.UtcNow.AddDays(pkg.ValidityDays).ToString("o"),
            IsActive = true,
            Transactions = new List<ClassCreditUsage>(),
        };

        _clientPackages[$"{tenantId}:{clientPkg.Id}"] = clientPkg;
        _logger.LogInformation("Client {ClientId} purchased package {PackageName}", request.ClientId, pkg.Name);

        return Ok(ApiResponse<object>.Ok(clientPkg));
    }

    /// <summary>POST /api/v1/classpackages/client/{clientId}/use — use a credit for a class</summary>
    [HttpPost("client/{clientId}/use")]
    public IActionResult UseCredit(string clientId, [FromBody] UseCreditRequest request)
    {
        var tenantId = GetTenantId().ToString();
        var clientPkg = _clientPackages.Values.FirstOrDefault(cp =>
            cp.TenantId == tenantId && cp.ClientId == clientId && cp.IsActive && cp.RemainingCredits > 0
            && (cp.PackageId == request.PackageId || request.PackageId == null)
            && DateTime.Parse(cp.ExpiresAt) > DateTime.UtcNow);

        if (clientPkg == null) return BadRequest(ApiResponse.Fail("No active package with credits found for this client"));

        clientPkg.UsedCredits++;
        clientPkg.RemainingCredits--;

        clientPkg.Transactions.Add(new ClassCreditUsage
        {
            Id = Guid.NewGuid().ToString(),
            ClassId = request.ClassId ?? "unknown",
            ClassName = request.ClassName,
            UsedAt = DateTime.UtcNow.ToString("o"),
            CreditsUsed = 1,
        });

        if (clientPkg.RemainingCredits == 0) clientPkg.IsActive = false;

        return Ok(ApiResponse<object>.Ok(new
        {
            packageId = clientPkg.Id,
            remainingCredits = clientPkg.RemainingCredits,
            usedCredits = clientPkg.UsedCredits,
            isExhausted = clientPkg.RemainingCredits == 0,
        }));
    }

    /// <summary>GET /api/v1/classpackages/client/{clientId} — get client's packages</summary>
    [HttpGet("client/{clientId}")]
    public IActionResult GetClientPackages(string clientId)
    {
        var tenantId = GetTenantId().ToString();
        var packages = _clientPackages.Values
            .Where(cp => cp.TenantId == tenantId && cp.ClientId == clientId)
            .OrderByDescending(cp => cp.PurchasedAt)
            .ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            packages,
            total = packages.Count,
            active = packages.Count(cp => cp.IsActive),
            totalCreditsRemaining = packages.Where(cp => cp.IsActive).Sum(cp => cp.RemainingCredits),
        }));
    }
}

// ── DTOs & Models ──────────────────────────────────────────────────────────

public class ClassPackage
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public int ValidityDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
    public bool IsTransferable { get; set; }
    public string[] ApplicableClassIds { get; set; } = Array.Empty<string>();
    public string CreatedAt { get; set; } = string.Empty;
}

public class ClientPackage
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public int TotalCredits { get; set; }
    public int UsedCredits { get; set; }
    public int RemainingCredits { get; set; }
    public decimal PurchasePrice { get; set; }
    public string PurchasedAt { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<ClassCreditUsage> Transactions { get; set; } = new();
}

public class ClassCreditUsage
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string UsedAt { get; set; } = string.Empty;
    public int CreditsUsed { get; set; } = 1;
}

public class CreateClassPackageRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Credits { get; set; } = 5;
    public decimal Price { get; set; }
    public int? ValidityDays { get; set; }
    public bool IsTransferable { get; set; }
    public string[]? ApplicableClassIds { get; set; }
}

public class PurchasePackageRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
}

public class UseCreditRequest
{
    public string? PackageId { get; set; }
    public string? ClassId { get; set; }
    public string? ClassName { get; set; }
}
