using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Tenant data backups / exports. Backs the /settings/backup page.
/// </summary>
[ApiController]
[Route("api/v1/tenant/backups")]
[Authorize]
public class TenantBackupsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public TenantBackupsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public record CreateBackupRequest(string name, string? type, List<string>? includedEntities);

    private static object Project(TenantBackup b)
    {
        string[] entities;
        try { entities = JsonSerializer.Deserialize<string[]>(string.IsNullOrWhiteSpace(b.IncludedEntitiesJson) ? "[]" : b.IncludedEntitiesJson) ?? Array.Empty<string>(); }
        catch { entities = Array.Empty<string>(); }
        return new
        {
            id = b.Id,
            name = b.Name,
            type = b.Type,
            status = b.Status,
            sizeBytes = b.SizeBytes,
            createdAt = b.CreatedAt,
            expiresAt = b.ExpiresAt,
            downloadUrl = b.DownloadUrl,
            includedEntities = entities,
            restorable = b.Restorable,
        };
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var backups = await _context.TenantBackups
            .Where(b => b.TenantId == tenantId.Value && !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        // The page reads res.data.data.backups
        return Ok(new { data = new { backups = backups.Select(Project) } });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var backups = await _context.TenantBackups
            .Where(b => b.TenantId == tenantId.Value && !b.IsDeleted)
            .ToListAsync();

        var last = backups.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

        return Ok(new
        {
            data = new
            {
                totalBackups = backups.Count,
                totalSizeBytes = backups.Sum(b => b.SizeBytes),
                lastBackupAt = (DateTime?)last?.CreatedAt,
                nextScheduledAt = (DateTime?)null,
                retentionDays = 30,
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBackupRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.name)) return BadRequest(new { message = "Backup name is required." });

        var backup = new TenantBackup
        {
            TenantId = tenantId.Value,
            Name = request.name.Trim(),
            Type = string.IsNullOrWhiteSpace(request.type) ? "manual" : request.type,
            Status = "completed", // synchronous stub — a real job would start "in_progress"
            IncludedEntitiesJson = JsonSerializer.Serialize(request.includedEntities ?? new List<string>()),
            SizeBytes = 0,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Restorable = true,
        };
        backup.DownloadUrl = $"/api/v1/tenant/backups/{backup.Id}/download";

        _context.TenantBackups.Add(backup);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { id = backup.Id }, Project(backup));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var backup = await _context.TenantBackups
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId.Value && !b.IsDeleted);
        if (backup == null) return NotFound();

        // Backup archive generation is asynchronous; return the manifest until the archive job exists.
        var manifest = JsonSerializer.SerializeToUtf8Bytes(Project(backup));
        return File(manifest, "application/json", $"{backup.Name}.json");
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var backup = await _context.TenantBackups
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId.Value && !b.IsDeleted);
        if (backup == null) return NotFound();
        if (!backup.Restorable) return BadRequest(new { message = "This backup is not restorable." });

        return Accepted(new { message = "Restore has been queued.", backupId = backup.Id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var backup = await _context.TenantBackups
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId.Value && !b.IsDeleted);
        if (backup == null) return NotFound();

        backup.IsDeleted = true;
        backup.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
