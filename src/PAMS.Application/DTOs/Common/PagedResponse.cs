namespace PAMS.Application.DTOs.Common;

/// <summary>
/// Standard paginated response wrapper per NFR-18.
/// </summary>
public sealed record PagedResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = new();
}

/// <summary>
/// Pagination metadata.
/// </summary>
public sealed record PaginationMeta
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages { get; init; }

    public static PaginationMeta Create(int page, int limit, int totalRecords)
        => new()
        {
            Page = page,
            Limit = limit,
            TotalRecords = totalRecords,
            TotalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling((double)totalRecords / limit)
        };
}
