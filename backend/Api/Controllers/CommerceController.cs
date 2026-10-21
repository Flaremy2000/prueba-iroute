using IRoute.Api.Contracts;
using IRoute.Application.Abstractions;
using IRoute.Application.Commerce;
using IRoute.Application.Quarantine;
using Microsoft.AspNetCore.Mvc;

namespace IRoute.Api.Controllers;

[ApiController]
[Route("api/commerce")]
[Produces("application/json")]
public sealed class CommerceController(
    ICommerceCommandService commandService,
    IQuarantineQueryService queryService) : ControllerBase
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    [ProducesResponseType<UploadCommerceResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadCommerceResult>> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo requerido",
                Detail = "Debes enviar un archivo CSV no vacío."
            });
        }

        await using var stream = file.OpenReadStream();

        var result = await commandService.UploadAsync(
            new UploadCommerceCommand(stream, file.FileName),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("process")]
    [ProducesResponseType<ProcessCommerceResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcessCommerceResult>> Process(
        [FromBody] ProcessCommerceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await commandService.ProcessAsync(
            new ProcessCommerceCommand(request.ProcessDate),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("quarantine")]
    [ProducesResponseType<PagedResult<QuarantineItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
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
