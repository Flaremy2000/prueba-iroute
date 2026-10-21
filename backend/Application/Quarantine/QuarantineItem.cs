namespace IRoute.Application.Quarantine;

/// <summary>
/// Registro rechazado durante el procesamiento.
/// </summary>
/// <param name="Id">Identificador del registro en cuarentena.</param>
/// <param name="MerchantName">Nombre del comercio.</param>
/// <param name="DocumentNumber">Número de documento.</param>
/// <param name="ProcessDate">Fecha de proceso.</param>
/// <param name="Motivo">Motivo por el que el registro fue rechazado.</param>
public sealed record QuarantineItem(
    long Id,
    string? MerchantName,
    string? DocumentNumber,
    DateOnly? ProcessDate,
    string Motivo);
