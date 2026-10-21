using IRoute.Api.Contracts;
using IRoute.Application.Abstractions;
using IRoute.Application.Commerce;
using IRoute.Application.Quarantine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IRoute.Api.Controllers;

/// <summary>
/// Carga, procesamiento y consulta de comercios.
/// </summary>
[ApiController]
[Authorize]
[Route("api/commerce")]
[Tags("Commerce")]
[Produces("application/json")]
public sealed class CommerceController(
    ICommerceCommandService commandService,
    IQuarantineQueryService queryService) : ControllerBase
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    /// <summary>
    /// Carga un archivo CSV con registros de comercio.
    /// </summary>
    /// <remarks>
    /// El archivo debe llamarse `commerce_DDMMYYYY.csv` y contener las columnas
    /// `pc_nomcomred`, `pc_numdoc` y `pc_processdate`.
    /// </remarks>
    /// <param name="request">Formulario con el archivo CSV.</param>
    /// <response code="200">Archivo cargado correctamente.</response>
    /// <response code="400">Archivo inválido, vacío o con datos incorrectos.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="409">El archivo ya fue cargado anteriormente.</response>
    /// <response code="415">El contenido no es un formulario multipart.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    [ProducesResponseType<UploadCommerceResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadCommerceResult>> Upload(
        [FromForm] UploadCommerceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo requerido",
                Detail = "Debes enviar un archivo CSV no vacío."
            });
        }

        await using var stream = request.File.OpenReadStream();

        var result = await commandService.UploadAsync(
            new UploadCommerceCommand(stream, request.File.FileName),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Procesa los comercios cargados para una fecha.
    /// </summary>
    /// <remarks>
    /// Los registros inválidos se mueven a cuarentena y los válidos permanecen
    /// en la tabla de comercios. El proceso es idempotente por fecha.
    /// </remarks>
    /// <param name="request">Datos del proceso.</param>
    /// <response code="200">Proceso ejecutado correctamente.</response>
    /// <response code="400">La fecha es obligatoria o tiene un formato inválido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="415">El contenido no es JSON.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("process")]
    [Consumes("application/json")]
    [ProducesResponseType<ProcessCommerceResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessCommerceResult>> Process(
        [FromBody] ProcessCommerceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await commandService.ProcessAsync(
            new ProcessCommerceCommand(request.ProcessDate),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Consulta los registros que quedaron en cuarentena.
    /// </summary>
    /// <param name="request">Filtros, búsqueda y paginación.</param>
    /// <response code="200">Consulta ejecutada correctamente.</response>
    /// <response code="400">Los filtros o la paginación son inválidos.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("quarantine")]
    [ProducesResponseType<PagedResult<QuarantineItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<QuarantineItem>>> GetQuarantine(
        [FromQuery] GetQuarantineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await queryService.GetAsync(
            new GetQuarantineQuery(
                request.ProcessDate,
                request.Search,
                request.Page,
                request.PageSize),
            cancellationToken);

        return Ok(result);
    }
}
