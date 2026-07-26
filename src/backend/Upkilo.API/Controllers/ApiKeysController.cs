using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Security.Cryptography;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

/// <summary>
/// API Keys controller for managing API access tokens for external integrations.
/// </summary>
[ApiController]
[Route("api/api-keys")]
[Authorize]
[RequiresFeature("ApiAccess")]
public class ApiKeysController : ControllerBase
{
    private readonly ILogger<ApiKeysController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ApiKeysController(
        ILogger<ApiKeysController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all API keys for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApiKeys()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var keys = await _context.ApiKeys
            .Where(k => k.TenantId == tenantId.Value && k.IsActive && !k.IsDeleted)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.Prefix,
                lastFourChars = k.LastFourChars,
                permissions = k.Scopes,
                lastUsedAt = k.LastUsedAt,
                createdAt = k.CreatedAt,
                expiresAt = k.ExpiresAt,
                isActive = k.IsActive
            })
            .ToListAsync();

        return Ok(new { data = keys });
    }

    /// <summary>
    /// Create new API key
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] NewApiKeyRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Generate a secure random key
        // Format: upk_{env}_{random_32_chars}
        var env = "live"; // or test, based on config
        var randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var randomString = Convert.ToBase64String(randomBytes)
            .Replace("+", "").Replace("/", "").Replace("=", "");
        
        var prefix = $"upk_{env}_";
        var fullKey = $"{prefix}{randomString}";
        var lastFour = fullKey.Substring(fullKey.Length - 4);

        // Hash the key for storage (SHA256)
        var keyHash = ComputeSha256Hash(fullKey);

        var apiKey = new ApiKey
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Prefix = prefix,
            KeyHash = keyHash,
            LastFourChars = lastFour,
            Scopes = request.Permissions,
            ExpiresAt = request.ExpiresAt,
            IsActive = true
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key created: {Name} ({Id})", request.Name, apiKey.Id);

        return Ok(new
        {
            id = apiKey.Id,
            name = apiKey.Name,
            key = fullKey, // ONLY shown once
            permissions = apiKey.Scopes,
            createdAt = apiKey.CreatedAt,
            warning = "This is the only time the full API key will be shown. Please save it securely."
        });
    }

    /// <summary>
    /// Update API key (permissions/name)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] UpdateApiKeyRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId.Value && !k.IsDeleted);

        if (apiKey == null) return NotFound();

        if (request.Name != null) apiKey.Name = request.Name;
        if (request.Permissions != null) apiKey.Scopes = request.Permissions;
        if (request.IsActive.HasValue) apiKey.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        _logger.LogInformation("API key updated: {KeyId}", id);

        return Ok(new { success = true });
    }

    /// <summary>
    /// Revoke API key (Soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId.Value && !k.IsDeleted);

        if (apiKey == null) return NotFound();

        apiKey.IsDeleted = true;
        apiKey.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key revoked: {KeyId}", id);
        return NoContent();
    }

    /// <summary>
    /// Revoke all API keys for the current tenant
    /// </summary>
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAllApiKeys()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var apiKeys = await _context.ApiKeys
            .Where(k => k.TenantId == tenantId.Value && !k.IsDeleted && k.IsActive)
            .ToListAsync();

        foreach (var key in apiKeys)
        {
            key.IsDeleted = true;
            key.IsActive = false;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("All API keys revoked for tenant: {TenantId}", tenantId.Value);

        return Ok(new { success = true, revokedCount = apiKeys.Count });
    }

    /// <summary>
    /// Rotate API key (Create new, expire old)
    /// </summary>
    [HttpPost("{id}/rotate")]
    public async Task<IActionResult> RotateApiKey(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var oldKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId.Value && !k.IsDeleted);

        if (oldKey == null) return NotFound();

        // 1. Generate new key
        var env = "live"; 
        var randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var randomString = Convert.ToBase64String(randomBytes)
            .Replace("+", "").Replace("/", "").Replace("=", "");
        
        var prefix = $"upk_{env}_";
        var fullKey = $"{prefix}{randomString}";
        var lastFour = fullKey.Substring(fullKey.Length - 4);
        var keyHash = ComputeSha256Hash(fullKey);

        // 2. Create new entity
        var newApiKey = new ApiKey
        {
            TenantId = tenantId.Value,
            Name = $"{oldKey.Name} (Rotated)",
            Prefix = prefix,
            KeyHash = keyHash,
            LastFourChars = lastFour,
            Scopes = new List<string>(oldKey.Scopes),
            ExpiresAt = oldKey.ExpiresAt, // Keep same expiry or extend?
            IsActive = true
        };

        // 3. Set a 24-hour grace period for the old key
        oldKey.IsActive = false;
        oldKey.GracePeriodExpiresAt = DateTime.UtcNow.AddHours(24);
        oldKey.UpdatedAt = DateTime.UtcNow;

        _context.ApiKeys.Add(newApiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key rotated: {OldKeyId} -> {NewKeyId}", id, newApiKey.Id);

        return Ok(new
        {
            success = true,
            newKey = fullKey,
            warning = "The old key has been deactivated immediately."
        });
    }

    /// <summary>
    /// Get API key usage statistics — queries AuditLog for real usage data.
    /// </summary>
    [HttpGet("{id}/usage")]
    public async Task<IActionResult> GetUsage(Guid id, [FromQuery] string period = "7d")
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify key belongs to tenant
        var keyExists = await _context.ApiKeys
            .AnyAsync(k => k.Id == id && k.TenantId == tenantId.Value && !k.IsDeleted);
        if (!keyExists) return NotFound();

        // Parse period
        var days = period switch
        {
            "24h" => 1,
            "7d" => 7,
            "30d" => 30,
            "90d" => 90,
            _ => 7
        };

        var since = DateTime.UtcNow.AddDays(-days);

        // Query AuditEntry for this API Key's activity in the period
        // The middleware sets UserId = ApiKey.Id for requests authenticated via API Key
        var logs = await _context.AuditEntries
            .Where(a => a.TenantId == tenantId.Value && a.UserId == id && a.Timestamp >= since)
            .ToListAsync();

        var totalRequests = logs.Count;
        var successfulRequests = logs.Count(l => l.Action != "Error" && l.Action != "Failed");
        var failedRequests = logs.Count(l => l.Action == "Error" || l.Action == "Failed");

        // Daily usage breakdown
        var dailyUsage = logs
            .GroupBy(l => l.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                requests = g.Count(),
                success = g.Count(l => l.Action != "Error" && l.Action != "Failed"),
                errors = g.Count(l => l.Action == "Error" || l.Action == "Failed")
            })
            .ToList();

        // Top endpoints by entity type
        var topEndpoints = logs
            .GroupBy(l => $"{l.Action} {l.EntityType}")
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new
            {
                endpoint = g.Key,
                count = g.Count()
            })
            .ToList();

        return Ok(new
        {
            keyId = id,
            period,
            totalRequests,
            successfulRequests,
            failedRequests,
            errorRate = totalRequests > 0 ? Math.Round((double)failedRequests / totalRequests * 100, 2) : 0,
            averageLatency = CalculateAverageLatency(logs),
            dailyUsage,
            topEndpoints
        });
    }

    private static double CalculateAverageLatency(List<Upkilo.Core.Entities.AuditEntry> logs)
    {
        var latencies = new List<long>();
        foreach (var log in logs)
        {
            if (string.IsNullOrEmpty(log.NewValues)) continue;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(log.NewValues);
                if (doc.RootElement.TryGetProperty("latencyMs", out var latencyProp))
                {
                    latencies.Add(latencyProp.GetInt64());
                }
            }
            catch { /* Skip malformed entries */ }
        }
        return latencies.Count > 0 ? Math.Round(latencies.Average(), 2) : 0;
    }

    /// <summary>
    /// Get available permissions for API keys
    /// </summary>
    [HttpGet("permissions")]
    public IActionResult GetPermissions()
    {
        var permissions = new[]
        {
            new { scope = "read:bookings", name = "Read Bookings", description = "View booking data" },
            new { scope = "write:bookings", name = "Write Bookings", description = "Create and update bookings" },
            new { scope = "read:clients", name = "Read Clients", description = "View client data" },
            new { scope = "write:clients", name = "Write Clients", description = "Create and update clients" },
            new { scope = "read:services", name = "Read Services", description = "View services" },
            new { scope = "write:services", name = "Write Services", description = "Manage services" },
            new { scope = "read:staff", name = "Read Staff", description = "View staff data" },
            new { scope = "write:staff", name = "Write Staff", description = "Manage staff" },
            new { scope = "read:payments", name = "Read Payments", description = "View payment data" },
            new { scope = "write:payments", name = "Write Payments", description = "Process payments" },
            new { scope = "read:analytics", name = "Read Analytics", description = "View analytics and reports" },
            new { scope = "webhooks", name = "Webhooks", description = "Receive webhook events" },
            new { scope = "read:all", name = "Read All", description = "Full read access" },
            new { scope = "write:all", name = "Write All", description = "Full write access" }
        };

        return Ok(new { data = permissions });
    }

    /// <summary>
    /// C6: GET /api/api-keys/expiring — returns keys expiring within the next 30 days.
    /// Frontend uses this to show renewal reminders in the Developer settings page.
    /// </summary>
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringKeys([FromQuery] int daysAhead = 30)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var cutoff = DateTime.UtcNow.AddDays(daysAhead);

        var expiring = await _context.ApiKeys
            .Where(k => k.TenantId == tenantId.Value && k.IsActive && !k.IsDeleted
                        && k.ExpiresAt.HasValue && k.ExpiresAt.Value <= cutoff)
            .OrderBy(k => k.ExpiresAt)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.Prefix,
                k.LastFourChars,
                k.ExpiresAt,
                daysUntilExpiry = (int)((k.ExpiresAt!.Value - DateTime.UtcNow).TotalDays),
                urgency = (k.ExpiresAt.Value - DateTime.UtcNow).TotalDays <= 7 ? "critical" :
                          (k.ExpiresAt.Value - DateTime.UtcNow).TotalDays <= 14 ? "warning" : "info"
            })
            .ToListAsync();

        return Ok(new
        {
            expiringKeys = expiring,
            totalExpiring = expiring.Count,
            criticalCount = expiring.Count(k => k.urgency == "critical"),
            message = expiring.Count > 0
                ? $"{expiring.Count} API key(s) expiring within {daysAhead} days. Rotate them to avoid service disruption."
                : "All API keys are valid.",
            rotateUrl = "/settings/developer/api-keys"
        });
    }

    /// <summary>
    /// C6: GET /api/api-keys/stale — Returns API keys older than 90 days with no expiry.
    /// Zero stale keys is the production target.
    /// </summary>
    [HttpGet("stale")]
    public async Task<IActionResult> GetStaleKeys([FromQuery] int ageDays = 90)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var cutoff = DateTime.UtcNow.AddDays(-ageDays);

        var stale = await _context.ApiKeys
            .Where(k => k.TenantId == tenantId.Value && k.IsActive && !k.IsDeleted
                        && k.CreatedAt <= cutoff
                        && !k.ExpiresAt.HasValue)
            .OrderBy(k => k.CreatedAt)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.Prefix,
                k.LastFourChars,
                k.CreatedAt,
                ageInDays = (int)(DateTime.UtcNow - k.CreatedAt).TotalDays,
                recommendation = "Set an expiry date or rotate this key."
            })
            .ToListAsync();

        return Ok(new
        {
            staleKeys = stale,
            totalStale = stale.Count,
            threshold = $"Keys older than {ageDays} days with no expiry",
            message = stale.Count > 0
                ? $"{stale.Count} stale API key(s) found. Rotate or set expiry dates."
                : "No stale API keys found.",
            rotateUrl = "/settings/developer/api-keys"
        });
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

// Request DTOs
public class NewApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public List<string>? Permissions { get; set; }
    public bool? IsActive { get; set; }
}
