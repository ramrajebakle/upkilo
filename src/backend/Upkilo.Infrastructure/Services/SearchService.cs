using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Search service implementation using PostgreSQL Full-Text Search.
/// </summary>
public class SearchService : ISearchService
{
    private readonly AppDbContext _context;

    public SearchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<T>> SearchAsync<T>(string query, Guid tenantId) where T : class
    {
        // This is a simplified pattern for PG Full-Text Search.
        // In reality, you'd use EF.Functions.ToTsVector and EF.Functions.ToTsQuery.
        // We ensure tenant isolation for all searches.
        
        var dbSet = _context.Set<T>();
        
        if (typeof(T) == typeof(Upkilo.Core.Entities.Booking))
        {
            return (IEnumerable<T>)await _context.Bookings
                .Where(b => b.TenantId == tenantId)
                .Where(b => EF.Functions.ToTsVector("english", b.Notes + " " + b.CustomerName)
                    .Matches(EF.Functions.ToTsQuery("english", query.Replace(" ", " & "))))
                .ToListAsync();
        }

        if (typeof(T) == typeof(Upkilo.Core.Entities.Client))
        {
            return (IEnumerable<T>)await _context.Clients
                .Where(c => c.TenantId == tenantId)
                .Where(c => EF.Functions.ToTsVector("english", c.FirstName + " " + c.LastName + " " + c.Email)
                    .Matches(EF.Functions.ToTsQuery("english", query.Replace(" ", " & "))))
                .ToListAsync();
        }

        return Enumerable.Empty<T>();
    }
}
