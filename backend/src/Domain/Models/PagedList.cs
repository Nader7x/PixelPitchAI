using Microsoft.EntityFrameworkCore;

namespace Domain.Models;

public class PagedList<T>
{
    /// <summary>
    /// The current page number.
    /// </summary>
    public int CurrentPage { get; private set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// The number of records on each page.
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// The total count of records in the entire collection.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// A flag indicating if there is a previous page.
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// A flag indicating if there is a next page.
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>
    /// The actual list of items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; private set; }

    public PagedList(IReadOnlyList<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize;
        CurrentPage = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }

    /// <summary>
    /// Creates a paginated list from a queryable source.
    /// </summary>
    /// <param name="source">The IQueryable source to paginate from.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A PagedList containing the items and pagination metadata.</returns>
    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize
    )
    {
        // Get the total count of items. This executes a COUNT(*) query on the database.
        var count = await source.CountAsync();

        // Retrieve the items for the specific page.
        // This executes a SELECT query with OFFSET and FETCH/LIMIT.
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
