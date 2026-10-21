namespace IRoute.Application.Quarantine;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);
