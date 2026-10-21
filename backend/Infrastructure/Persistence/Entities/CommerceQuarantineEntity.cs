namespace IRoute.Infrastructure.Persistence.Entities;

public sealed class CommerceQuarantineEntity
{
    public long Id { get; set; }
    public string? MerchantName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? ProcessDate { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
