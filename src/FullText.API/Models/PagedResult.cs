using Microsoft.EntityFrameworkCore;

namespace FullText.API.Models;

public sealed record class PagedResult<T>
{
    private PagedResult(
        int page,
        int pageSize,
        int totalCount,
        int totalPages,
        IReadOnlyList<T> items)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalPages;
        Items = items;
    }

    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public IReadOnlyList<T> Items { get; }
    public bool HasNext => Page * PageSize < TotalCount;
    public bool HasPrevious => Page > 1;

    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 10);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        page = Math.Clamp(page, 1, totalPages);

        var items = await query.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(cancellationToken);

        return new PagedResult<T>(page, pageSize, totalCount, totalPages, items);
    }
}
