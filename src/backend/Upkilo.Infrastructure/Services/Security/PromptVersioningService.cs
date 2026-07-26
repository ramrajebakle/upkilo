using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services.Security;

/// <summary>
/// Manages prompt versions with CRUD, activation, and rollback capabilities.
/// Provides multi-tenant prompt isolation for AI agent configurations.
/// </summary>
public class PromptVersioningService : IPromptVersioningService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PromptVersioningService> _logger;

    public PromptVersioningService(AppDbContext context, ILogger<PromptVersioningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the currently active prompt version for a given key and tenant.
    /// Falls back to the global (null tenant) prompt if no tenant-specific one exists.
    /// </summary>
    public async Task<PromptVersion?> GetActivePromptAsync(string promptKey, Guid tenantId)
    {
        // First try tenant-specific
        var prompt = await _context.PromptVersions
            .Where(p => p.PromptKey == promptKey && p.TenantId == tenantId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (prompt != null) return prompt;

        // Fallback to global (system-level) prompts
        _logger.LogDebug("No tenant-specific prompt found for key {Key}, using system default", promptKey);
        return await _context.PromptVersions
            .Where(p => p.PromptKey == promptKey && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets all versions of a prompt for audit/management purposes.
    /// </summary>
    public async Task<List<PromptVersion>> GetVersionHistoryAsync(string promptKey, Guid tenantId)
    {
        return await _context.PromptVersions
            .Where(p => p.PromptKey == promptKey && p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a new prompt version. Automatically deactivates the previous active version.
    /// </summary>
    public async Task<PromptVersion> CreateVersionAsync(PromptVersion newVersion)
    {
        // Deactivate all other active versions for this key+tenant
        var existingActive = await _context.PromptVersions
            .Where(p => p.PromptKey == newVersion.PromptKey
                     && p.TenantId == newVersion.TenantId
                     && p.IsActive)
            .ToListAsync();

        foreach (var existing in existingActive)
        {
            existing.IsActive = false;
        }

        newVersion.Id = Guid.NewGuid();
        newVersion.IsActive = true;
        newVersion.ActivatedAt = DateTime.UtcNow;
        newVersion.CreatedAt = DateTime.UtcNow;

        _context.PromptVersions.Add(newVersion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created prompt version {Version} for key {Key}, tenant {TenantId}",
            newVersion.Version, newVersion.PromptKey, newVersion.TenantId);

        return newVersion;
    }

    /// <summary>
    /// Rolls back to a specific previous version by ID. Deactivates the current active version.
    /// </summary>
    public async Task<PromptVersion?> RollbackToVersionAsync(Guid versionId, Guid tenantId)
    {
        var targetVersion = await _context.PromptVersions
            .FirstOrDefaultAsync(p => p.Id == versionId && p.TenantId == tenantId);

        if (targetVersion == null)
        {
            _logger.LogWarning("Rollback target version {VersionId} not found for tenant {TenantId}", versionId, tenantId);
            return null;
        }

        // Deactivate current active version
        var currentActive = await _context.PromptVersions
            .Where(p => p.PromptKey == targetVersion.PromptKey
                     && p.TenantId == tenantId
                     && p.IsActive)
            .ToListAsync();

        foreach (var active in currentActive)
        {
            active.IsActive = false;
            active.RolledBackAt = DateTime.UtcNow;
        }

        // Reactivate target version
        targetVersion.IsActive = true;
        targetVersion.ActivatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Rolled back prompt {Key} to version {Version} for tenant {TenantId}",
            targetVersion.PromptKey, targetVersion.Version, tenantId);

        return targetVersion;
    }

    /// <summary>
    /// Gets all distinct prompt keys in the system (for management UI).
    /// </summary>
    public async Task<List<string>> GetPromptKeysAsync(Guid tenantId)
    {
        return await _context.PromptVersions
            .Where(p => p.TenantId == tenantId)
            .Select(p => p.PromptKey)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Applies variable substitution to a prompt template.
    /// Replaces {{variableName}} placeholders with provided values.
    /// </summary>
    public static string ApplyTemplate(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return template;

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }
}
