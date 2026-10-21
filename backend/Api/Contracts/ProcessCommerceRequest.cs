namespace IRoute.Api.Contracts;

/// <summary>
/// Datos necesarios para procesar los comercios de una fecha.
/// </summary>
public sealed class ProcessCommerceRequest
{
    /// <summary>
    /// Fecha de proceso que se aplicará a los registros cargados.
    /// </summary>
    /// <example>2026-11-02</example>
    public DateOnly ProcessDate { get; init; }
}
