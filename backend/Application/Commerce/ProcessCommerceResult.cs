namespace IRoute.Application.Commerce;

/// <summary>
/// Resultado del procesamiento de una fecha.
/// </summary>
/// <param name="ProcessDate">Fecha procesada.</param>
/// <param name="Quarantined">Cantidad de registros enviados a cuarentena.</param>
/// <param name="Remaining">Cantidad de registros válidos que permanecen en comercios.</param>
public sealed record ProcessCommerceResult(
    DateOnly ProcessDate,
    int Quarantined,
    int Remaining);
