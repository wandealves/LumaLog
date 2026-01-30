namespace LumaLog.Models;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Empty(int page = 1, int pageSize = 50) => new()
    {
        Items = new List<T>(),
        Page = page,
        PageSize = pageSize,
        TotalItems = 0
    };

    public static PagedResult<T> Create(List<T> items, int page, int pageSize, int totalItems) => new()
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems
    };
}
