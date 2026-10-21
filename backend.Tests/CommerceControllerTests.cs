using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IRoute.Application.Abstractions;
using IRoute.Application.Commerce;
using IRoute.Application.Quarantine;
using IRoute.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IRoute.Tests;

public sealed class CommerceControllerTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    [Fact]
    public async Task Upload_WithoutFile_Returns400()
    {
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new StringContent("valor"), "other" }
        };

        var response = await client.PostAsync("/api/commerce/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Debes enviar un archivo CSV no vacío.", body);
    }

    [Fact]
    public async Task Upload_WithFile_ReturnsResult()
    {
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes("csv"));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", "commerce_01112026.csv");

        var response = await client.PostAsync("/api/commerce/upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UploadCommerceResult>();
        Assert.NotNull(result);
        Assert.Equal("commerce_01112026.csv", result.FileName);
        Assert.Equal(1, result.RowsInserted);
    }

    [Fact]
    public async Task Process_ValidDate_ReturnsResult()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/commerce/process",
            new { processDate = "2026-11-02" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProcessCommerceResult>();
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 11, 2), result.ProcessDate);
        Assert.Equal(1, result.Quarantined);
        Assert.Equal(2, result.Remaining);
    }

    [Fact]
    public async Task Process_WithoutDate_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/commerce/process",
            new { processDate = "0001-01-01" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("fecha de proceso", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Quarantine_ReturnsPagedResult()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/commerce/quarantine?page=2&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<QuarantineItem>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task Quarantine_InvalidPage_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/commerce/quarantine?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_ContainsDocumentationMetadata()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("i-Route Commerce API", json);
        Assert.Contains("/api/commerce/upload", json);
        Assert.Contains("Carga un archivo CSV con registros de comercio.", json);
    }

    [Fact]
    public async Task Scalar_ReturnsHtml()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/scalar/v1");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("scalar", html, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _factory.Dispose();

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public FakeCommandService Command { get; } = new();
        public FakeQueryService Query { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICommerceCommandService>();
                services.RemoveAll<IQuarantineQueryService>();
                services.AddSingleton<ICommerceCommandService>(Command);
                services.AddSingleton<IQuarantineQueryService>(Query);
            });
        }
    }

    private sealed class FakeCommandService : ICommerceCommandService
    {
        public Task<UploadCommerceResult> UploadAsync(
            UploadCommerceCommand command,
            CancellationToken cancellationToken) =>
            Task.FromResult(new UploadCommerceResult(
                Path.GetFileName(command.FileName),
                new DateOnly(2026, 11, 1),
                1,
                1));

        public Task<ProcessCommerceResult> ProcessAsync(
            ProcessCommerceCommand command,
            CancellationToken cancellationToken)
        {
            if (command.ProcessDate == default)
            {
                throw new DomainValidationException("La fecha de proceso es obligatoria.");
            }

            return Task.FromResult(new ProcessCommerceResult(command.ProcessDate, 1, 2));
        }
    }

    private sealed class FakeQueryService : IQuarantineQueryService
    {
        public Task<PagedResult<QuarantineItem>> GetAsync(
            GetQuarantineQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<QuarantineItem>(
                [],
                query.Page,
                query.PageSize,
                0));
    }
}
