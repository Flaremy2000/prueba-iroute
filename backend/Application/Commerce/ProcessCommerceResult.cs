namespace IRoute.Application.Commerce;

public sealed record ProcessCommerceResult(
    DateOnly ProcessDate,
    int Quarantined,
    int Remaining);
