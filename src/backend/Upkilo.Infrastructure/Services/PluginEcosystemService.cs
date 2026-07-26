using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class PluginEcosystemService
{
    private readonly ILogger<PluginEcosystemService> _logger;
    private readonly AppDbContext _context;
    private readonly IWebhookService? _webhookService;

    public PluginEcosystemService(
        ILogger<PluginEcosystemService> logger,
        AppDbContext context,
        IWebhookService? webhookService = null)
    {
        _logger = logger;
        _context = context;
        _webhookService = webhookService;
    }

    public async Task<PluginInstallResult> InstallPluginAsync(Guid tenantId, string pluginId)
    {
        _logger.LogInformation("Installing plugin {PluginId} for tenant {TenantId}", pluginId, tenantId);

        // 1. Fetch plugin definition (manifest)
        var definition = await _context.PluginDefinitions
            .FirstOrDefaultAsync(p => p.Slug == pluginId);

        if (definition == null)
        {
            _logger.LogWarning("Plugin {PluginId} not found in registry", pluginId);
            return new PluginInstallResult { Success = false, ErrorMessage = $"Plugin '{pluginId}' not found." };
        }

        // 2. Check if already installed
        var existing = await _context.PluginInstallations
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PluginId == definition.Id);

        if (existing != null)
        {
            if (existing.IsEnabled)
                return new PluginInstallResult { Success = true, RequiresReboot = false };

            existing.IsEnabled = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Plugin {PluginId} re-enabled for tenant {TenantId}", pluginId, tenantId);
            return new PluginInstallResult { Success = true, RequiresReboot = false };
        }

        // 3. Validate manifest scopes (basic check: manifest JSON must be present for non-free plugins)
        if (!definition.IsFree && string.IsNullOrEmpty(definition.ManifestJson))
        {
            return new PluginInstallResult { Success = false, ErrorMessage = "Plugin manifest is missing or invalid." };
        }

        // 4. Register installation in DB
        var installation = new PluginInstallation
        {
            TenantId = tenantId,
            PluginId = definition.Id,
            IsEnabled = true,
            InstalledAt = DateTime.UtcNow
        };
        _context.PluginInstallations.Add(installation);

        // 5. Increment global install counter
        definition.InstallCount++;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Plugin {PluginId} installed for tenant {TenantId}", pluginId, tenantId);

        return new PluginInstallResult { Success = true, RequiresReboot = false };
    }

    public async Task UninstallPluginAsync(Guid tenantId, string pluginId)
    {
        var definition = await _context.PluginDefinitions.FirstOrDefaultAsync(p => p.Slug == pluginId);
        if (definition == null) return;

        var installation = await _context.PluginInstallations
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PluginId == definition.Id);

        if (installation != null)
        {
            installation.IsEnabled = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Plugin {PluginId} disabled for tenant {TenantId}", pluginId, tenantId);
        }
    }

    public async Task EmitHookAsync(Guid tenantId, string eventName, object payload)
    {
        _logger.LogInformation("Emitting plugin hook {EventName} for tenant {TenantId}", eventName, tenantId);

        // Find all installed plugins that register webhooks for this event
        var installations = await _context.PluginInstallations
            .Include(i => i.Plugin)
            .Where(i => i.TenantId == tenantId && i.IsEnabled)
            .ToListAsync();

        foreach (var installation in installations)
        {
            if (string.IsNullOrEmpty(installation.Plugin?.ManifestJson)) continue;

            try
            {
                using var doc = JsonDocument.Parse(installation.Plugin.ManifestJson);
                if (!doc.RootElement.TryGetProperty("hooks", out var hooks)) continue;

                foreach (var hook in hooks.EnumerateArray())
                {
                    if (!hook.TryGetProperty("event", out var ev) || ev.GetString() != eventName) continue;
                    if (!hook.TryGetProperty("type", out var type)) continue;

                    if (type.GetString() == "webhook" && _webhookService != null)
                    {
                        await _webhookService.DispatchEventAsync(tenantId, eventName, payload);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process hooks for plugin {PluginId}", installation.PluginId);
            }
        }
    }

    public async Task<List<PluginInstallation>> GetInstalledPluginsAsync(Guid tenantId)
    {
        return await _context.PluginInstallations
            .Include(i => i.Plugin)
            .Where(i => i.TenantId == tenantId && i.IsEnabled)
            .ToListAsync();
    }
}

public class PluginInstallResult
{
    public bool Success { get; set; }
    public bool RequiresReboot { get; set; }
    public string? ErrorMessage { get; set; }
}
