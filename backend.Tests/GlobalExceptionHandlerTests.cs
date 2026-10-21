using System.Text.Json;
using IRoute.Api.Middleware;
using IRoute.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace IRoute.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationException_ReturnsProblemDetails400()
    {
        var context = CreateContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new DomainValidationException("Dato inválido."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Solicitud inválida", problem.Title);
        Assert.Equal("Dato inválido.", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ConflictException_ReturnsProblemDetails409()
    {
        var context = CreateContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new DomainConflictException("El archivo ya fue cargado."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflicto", problem.Title);
        Assert.Equal("El archivo ya fue cargado.", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_ReturnsGenericProblemDetails500()
    {
        var context = CreateContext();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Detalle interno."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Error interno del servidor", problem.Title);
        Assert.DoesNotContain("Detalle interno.", problem.Detail);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/commerce/upload";
        return context;
    }

    private static async Task<ProblemDetailsDto> ReadProblemAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetailsDto>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private sealed class ProblemDetailsDto
    {
        public int? Status { get; init; }
        public string? Title { get; init; }
        public string? Detail { get; init; }
    }
}
