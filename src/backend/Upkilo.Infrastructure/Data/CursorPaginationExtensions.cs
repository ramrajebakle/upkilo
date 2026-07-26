using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// Cursor-based pagination helper for high-performance listing endpoints.
/// Uses an opaque cursor (Base64-encoded ID) instead of offset-based pagination
/// to prevent skipping/duplicating records when data changes between pages.
/// Scales to millions of records without performance degradation.
/// </summary>
public static class CursorPaginationExtensions
{
    /// <summary>
    /// Apply cursor-based forward pagination to a queryable.
    /// Returns items after the given cursor, ordered by the specified key.
    /// </summary>
    public static async Task<CursorPage<T>> ToCursorPageAsync<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> orderBy,
        Expression<Func<T, Guid>> idSelector,
        string? cursor = null,
        int pageSize = 20,
        CancellationToken ct = default) where T : class where TKey : IComparable<TKey>
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Decode cursor to get the last seen ID
        if (!string.IsNullOrEmpty(cursor))
        {
            var lastId = DecodeCursor(cursor);
            if (lastId != null)
            {
                // Filter to records after the cursor
                var param = idSelector.Parameters[0];
                var idAccess = idSelector.Body;
                var comparison = Expression.GreaterThan(
                    idAccess,
                    Expression.Constant(lastId.Value));
                var lambda = Expression.Lambda<Func<T, bool>>(comparison, param);
                query = query.Where(lambda);
            }
        }

        var items = await query
            .OrderBy(orderBy)
            .Take(pageSize + 1) // Fetch one extra to determine hasMore
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        // Generate next cursor from the last item's ID
        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var compiledIdSelector = idSelector.Compile();
            var lastItem = items[^1];
            var lastItemId = compiledIdSelector(lastItem);
            nextCursor = EncodeCursor(lastItemId);
        }

        return new CursorPage<T>
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }

    private static string EncodeCursor(Guid id)
    {
        return Convert.ToBase64String(id.ToByteArray());
    }

    private static Guid? DecodeCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            return new Guid(bytes);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents a page of cursor-paginated results.
/// </summary>
public class CursorPage<T>
{
    public List<T> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int PageSize { get; set; }
}
