namespace IRoute.Domain.Entities;

public sealed record CommerceRecord
{
    public string? MerchantName { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly ProcessDate { get; init; }
}
