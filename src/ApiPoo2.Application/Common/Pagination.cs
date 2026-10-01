namespace ApiPoo2.Application.Common;

public sealed record PagedFilter(
    int Page = 1,
    int PageSize = 20,
    string? OrderBy = null,
    bool Descending = false)
{
    public int Skip => Page <= 1 ? 0 : (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 || Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
