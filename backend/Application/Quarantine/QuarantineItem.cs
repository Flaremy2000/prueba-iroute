namespace IRoute.Application.Quarantine;

public sealed record QuarantineItem(
    long Id,
    string? MerchantName,
    string? DocumentNumber,
    DateOnly? ProcessDate,
    string Motivo);
