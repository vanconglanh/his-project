namespace ProDiabHis.Application.Common;

/// <summary>Ket qua phan trang</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

    public PagedResult() { }

    public PagedResult(IEnumerable<T> items, int page, int pageSize, int total)
    {
        Items = items.ToList().AsReadOnly();
        Page = page;
        PageSize = pageSize;
        Total = total;
    }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int total) =>
        new() { Items = items, Page = page, PageSize = pageSize, Total = total };
}
