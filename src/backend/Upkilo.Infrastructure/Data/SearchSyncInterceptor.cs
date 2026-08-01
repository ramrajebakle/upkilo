using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that automatically synchronizes entity changes with Elasticsearch.
/// </summary>
public class SearchSyncInterceptor : SaveChangesInterceptor
{
    private readonly IElasticsearchService _searchService;

    public SearchSyncInterceptor(IElasticsearchService searchService)
    {
        _searchService = searchService;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null) return result;

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is not TenantEntity tenantEntity) continue;

            // Only index specific entities for search
            if (entry.Entity is Client || entry.Entity is Booking || entry.Entity is Service)
            {
                var tenantId = tenantEntity.TenantId.ToString();

                if (entry.State == EntityState.Deleted)
                {
                    // Get Id via reflection or cast to BaseEntity if possible
                    if (entry.Entity is BaseEntity baseEntity)
                    {
                        await _searchService.DeleteEntityAsync<object>(tenantId, baseEntity.Id.ToString());
                    }
                }
                else
                {
                    await _searchService.IndexEntityAsync(tenantId, entry.Entity);
                }
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
