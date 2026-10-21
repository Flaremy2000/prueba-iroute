using System.ComponentModel.DataAnnotations;

namespace IRoute.Api.Contracts;

public sealed class GetQuarantineRequest
{
    public DateOnly? ProcessDate { get; init; }

    [StringLength(100)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
