namespace IRoute.Application.Quarantine;

public sealed record GetQuarantineQuery(
    DateOnly? ProcessDate,
    string? Search,
    int Page,
    int PageSize);
