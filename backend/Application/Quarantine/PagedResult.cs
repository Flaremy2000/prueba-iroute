namespace IRoute.Application.Quarantine;

/// <summary>
/// Resultado paginado de una consulta.
/// </summary>
/// <typeparam name="T">Tipo de elemento de la página.</typeparam>
/// <param name="Items">Elementos de la página solicitada.</param>
/// <param name="Page">Número de página.</param>
/// <param name="PageSize">Cantidad de elementos por página.</param>
/// <param name="Total">Total de elementos que cumplen el filtro.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);
