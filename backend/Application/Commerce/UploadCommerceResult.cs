namespace IRoute.Application.Commerce;

/// <summary>
/// Resultado de la carga de un archivo CSV.
/// </summary>
/// <param name="FileName">Nombre del archivo procesado.</param>
/// <param name="ProcessDate">Fecha obtenida del nombre del archivo.</param>
/// <param name="RowsRead">Cantidad de filas leídas del CSV.</param>
/// <param name="RowsInserted">Cantidad de filas insertadas en la base de datos.</param>
public sealed record UploadCommerceResult(
    string FileName,
    DateOnly ProcessDate,
    int RowsRead,
    int RowsInserted);
