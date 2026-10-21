namespace IRoute.Infrastructure.Persistence.Entities;

public sealed class CommerceEntity
{
    public long Id { get; set; }
    public string? MerchantName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? ProcessDate { get; set; }
}
