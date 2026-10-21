using System.ComponentModel.DataAnnotations;

namespace IRoute.Api.Contracts;

/// <summary>
/// Filtros y paginación para consultar registros en cuarentena.
/// </summary>
public sealed class GetQuarantineRequest
{
    /// <summary>
    /// Fecha de proceso a consultar. Si se omite, se devuelven todas las fechas.
    /// </summary>
    /// <example>2026-11-02</example>
    public DateOnly? ProcessDate { get; init; }

    /// <summary>
    /// Texto a buscar en el nombre del comercio o en el número de documento.
    /// </summary>
    /// <example>Comercio</example>
    [StringLength(100)]
    public string? Search { get; init; }

    /// <summary>
    /// Número de página, empezando en 1.
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Cantidad de registros por página, entre 1 y 100.
    /// </summary>
    /// <example>20</example>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
