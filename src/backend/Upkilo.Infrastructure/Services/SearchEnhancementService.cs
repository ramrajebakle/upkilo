using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for managing search enhancements like saved filters and recent search history.
/// </summary>
public class SearchEnhancementService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SearchEnhancementService> _logger;

    public SearchEnhancementService(AppDbContext context, ILogger<SearchEnhancementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // --- Recent Searches ---

    public async Task LogSearchAsync(Guid tenantId, Guid userId, string query, string searchType, int resultCount)
    {
        try
        {
            var recent = new RecentSearch
            {
                TenantId = tenantId,
                UserId = userId,
                QueryString = query,
                SearchType = searchType,
                ResultCount = resultCount,
                SearchedAt = DateTime.UtcNow
            };

            _context.Set<RecentSearch>().Add(recent);

            // Keep only last 20 searches per user
            var oldSearches = await _context.Set<RecentSearch>()
                .Where(r => r.TenantId == tenantId && r.UserId == userId)
                .OrderByDescending(r => r.SearchedAt)
                .Skip(19) // Will keep this + 19 = 20
                .ToListAsync();

            if (oldSearches.Any())
            {
                _context.Set<RecentSearch>().RemoveRange(oldSearches);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Fail silently for analytics tracking
            _logger.LogWarning(ex, "Failed to log recent search for User {UserId}", userId);
        }
    }

    public async Task<List<RecentSearch>> GetRecentSearchesAsync(Guid tenantId, Guid userId, int limit = 5)
    {
        return await _context.Set<RecentSearch>()
            .Where(r => r.TenantId == tenantId && r.UserId == userId)
            .OrderByDescending(r => r.SearchedAt)
            .Take(limit)
            .ToListAsync();
    }

    // --- Saved Searches ---

    public async Task<SavedSearchFilter> SaveSearchAsync(Guid tenantId, Guid userId, string name, string query, string searchType, string filtersJson)
    {
        var savedFilter = new SavedSearchFilter
        {
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            QueryString = query,
            SearchType = searchType,
            FiltersJson = filtersJson,
            LastUsedAt = DateTime.UtcNow
        };

        _context.Set<SavedSearchFilter>().Add(savedFilter);
        await _context.SaveChangesAsync();

        return savedFilter;
    }

    public async Task<List<SavedSearchFilter>> GetSavedSearchesAsync(Guid tenantId, Guid userId)
    {
        return await _context.Set<SavedSearchFilter>()
            .Where(s => s.TenantId == tenantId && s.UserId == userId)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync();
    }

    public async Task DeleteSavedSearchAsync(Guid savedSearchId, Guid tenantId, Guid userId)
    {
        var savedSearch = await _context.Set<SavedSearchFilter>()
            .FirstOrDefaultAsync(s => s.Id == savedSearchId && s.TenantId == tenantId && s.UserId == userId);

        if (savedSearch != null)
        {
            _context.Set<SavedSearchFilter>().Remove(savedSearch);
            await _context.SaveChangesAsync();
        }
    }
}
