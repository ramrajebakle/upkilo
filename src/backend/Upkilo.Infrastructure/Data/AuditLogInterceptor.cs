using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that automatically records audit trails for entity changes.
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogInterceptor(ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor)
    {
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null) return result;

        var auditEntries = CreateAuditEntries(eventData.Context);
        if (auditEntries.Any())
        {
            eventData.Context.Set<AuditEntry>().AddRange(auditEntries);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditEntry> CreateAuditEntries(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        var entries = new List<AuditEntry>();
        var tenantId = _tenantProvider.GetTenantId();
        var userId = _tenantProvider.GetUserId();
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEntry || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            if (entry.Entity is not TenantEntity tenantEntity) continue;

            var auditEntry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantEntity.TenantId,
                UserId = userId,
                EntityType = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            if (entry.Entity is BaseEntity baseEntity)
            {
                auditEntry.EntityId = baseEntity.Id.ToString();
            }

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var changedFields = new List<string>();

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    if (auditEntry.EntityId == null) auditEntry.EntityId = property.CurrentValue?.ToString() ?? "";
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                            changedFields.Add(propertyName);
                        }
                        break;
                }
            }

            auditEntry.OldValues = oldValues.Any() ? JsonSerializer.Serialize(oldValues) : null;
            auditEntry.NewValues = newValues.Any() ? JsonSerializer.Serialize(newValues) : null;
            auditEntry.ChangedFields = changedFields.Any() ? JsonSerializer.Serialize(changedFields) : null;

            entries.Add(auditEntry);
        }

        return entries;
    }
}
