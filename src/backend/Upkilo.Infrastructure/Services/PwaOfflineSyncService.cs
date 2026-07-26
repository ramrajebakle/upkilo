using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implements Task 1801: Offline caching strategy
/// Implements Task 1805: Background sync queue
/// Implements Task 1807: Conflict resolution
/// </summary>
public class PwaOfflineSyncService
{
    private readonly ILogger<PwaOfflineSyncService> _logger;
    private readonly AppDbContext _context;

    public PwaOfflineSyncService(ILogger<PwaOfflineSyncService> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<SyncResolutionResult> ProcessOfflineQueueAsync(Guid tenantId, List<OfflineMutation> mutations)
    {
        _logger.LogInformation("Task 1805: Processing {Count} background sync mutations for {TenantId}", mutations.Count, tenantId);

        int resolvedCount = 0;
        var conflicts = new List<string>();

        foreach (var mutation in mutations)
        {
            try
            {
                var (isConflict, serverVersion) = await GetServerVersionAsync(mutation);

                if (isConflict)
                {
                    _logger.LogWarning("Task 1807: Merge conflict on entity {EntityId} (clientV={ClientV} serverV={ServerV})",
                        mutation.EntityId, mutation.ClientVersion, serverVersion);

                    // Log to AuditEntries
                    _context.AuditEntries.Add(new AuditEntry
                    {
                        TenantId = tenantId,
                        EntityType = mutation.EntityType,
                        EntityId = mutation.EntityId,
                        Action = "SyncConflict",
                        Details = $"ClientVersion={mutation.ClientVersion}, ServerVersion={serverVersion}",
                        Timestamp = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    conflicts.Add(mutation.EntityId);
                    continue;
                }

                // Apply the mutation
                await ApplyMutationAsync(tenantId, mutation);
                resolvedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply mutation for entity {EntityId}", mutation.EntityId);
                conflicts.Add(mutation.EntityId);
            }
        }

        return new SyncResolutionResult
        {
            Resolved = resolvedCount,
            Conflicts = conflicts.Count,
            ConflictEntityIds = conflicts
        };
    }

    /// <summary>Returns (isConflict, serverVersion). Conflict when server version > client version.</summary>
    private async Task<(bool isConflict, int serverVersion)> GetServerVersionAsync(OfflineMutation mutation)
    {
        if (!Guid.TryParse(mutation.EntityId, out var entityGuid))
            return (false, 0);

        int serverVersion = 0;

        switch (mutation.EntityType)
        {
            case "Booking":
                var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == entityGuid);
                if (booking == null) return (false, 0);
                serverVersion = (int)(booking.UpdatedAt.Ticks % int.MaxValue);
                break;
            case "Client":
                var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == entityGuid);
                if (client == null) return (false, 0);
                serverVersion = (int)(client.UpdatedAt.Ticks % int.MaxValue);
                break;
            case "Invoice":
                var invoice = await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == entityGuid);
                if (invoice == null) return (false, 0);
                serverVersion = (int)(invoice.UpdatedAt.Ticks % int.MaxValue);
                break;
            default:
                return (false, 0);
        }

        return (serverVersion > mutation.ClientVersion, serverVersion);
    }

    private async Task ApplyMutationAsync(Guid tenantId, OfflineMutation mutation)
    {
        if (!Guid.TryParse(mutation.EntityId, out var entityGuid))
            return;

        using var doc = JsonDocument.Parse(mutation.PayloadJson);
        var root = doc.RootElement;

        switch (mutation.EntityType)
        {
            case "Booking":
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == entityGuid && b.TenantId == tenantId);
                if (booking == null) return;
                if (root.TryGetProperty("status", out var statusEl))
                {
                    if (Enum.TryParse<Upkilo.Core.Entities.BookingStatus>(statusEl.GetString(), true, out var status))
                        booking.Status = status;
                }
                if (root.TryGetProperty("notes", out var notesEl)) booking.Notes = notesEl.GetString();
                booking.UpdatedAt = DateTime.UtcNow;
                break;

            case "Client":
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == entityGuid && c.TenantId == tenantId);
                if (client == null) return;
                if (root.TryGetProperty("firstName", out var fn)) client.FirstName = fn.GetString();
                if (root.TryGetProperty("lastName", out var ln)) client.LastName = ln.GetString();
                if (root.TryGetProperty("email", out var em)) client.Email = em.GetString();
                if (root.TryGetProperty("phone", out var ph)) client.Phone = ph.GetString();
                client.UpdatedAt = DateTime.UtcNow;
                break;

            default:
                _logger.LogWarning("PwaOfflineSync: unsupported entity type {Type}", mutation.EntityType);
                return;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Applied offline mutation for {Type}/{Id}", mutation.EntityType, mutation.EntityId);
    }
}

public class OfflineMutation
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public int ClientVersion { get; set; }
    public int ServerVersion { get; set; }
}

public class SyncResolutionResult
{
    public int Resolved { get; set; }
    public int Conflicts { get; set; }
    public List<string> ConflictEntityIds { get; set; } = new();
}
