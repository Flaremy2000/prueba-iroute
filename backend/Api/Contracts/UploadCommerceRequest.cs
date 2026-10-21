using Microsoft.AspNetCore.Http;

namespace IRoute.Api.Contracts;

/// <summary>
/// Formulario para cargar un archivo CSV de comercios.
/// </summary>
public sealed class UploadCommerceRequest
{
    /// <summary>
    /// Archivo CSV con las columnas pc_nomcomred, pc_numdoc y pc_processdate.
    /// </summary>
    public IFormFile? File { get; init; }
}
