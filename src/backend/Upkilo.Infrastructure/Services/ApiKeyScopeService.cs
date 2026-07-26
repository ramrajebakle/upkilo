using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for validating API key scopes and permissions.
/// </summary>
public class ApiKeyScopeService : IApiKeyScopeService
{
    private readonly AppDbContext _context;

    public ApiKeyScopeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ValidateScopeAsync(string plainApiKey, string requiredScope)
    {
        var hashedKey = HashApiKey(plainApiKey);
        
        var key = await _context.Set<ApiKey>()
            .FirstOrDefaultAsync(k => k.KeyHash == hashedKey && k.IsActive);

        if (key == null) return false;

        // Check if key has expired
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
        {
            return false;
        }

        // Scope check logic: typical format "read:bookings", "write:clients" or "*"
        if (key.Scopes == null || key.Scopes.Count == 0) return false;
        
        if (key.Scopes.Contains("*")) return true;
        
        return key.Scopes.Any(s => s.Equals(requiredScope, StringComparison.OrdinalIgnoreCase));
    }

    private string HashApiKey(string key)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(bytes);
    }
}
