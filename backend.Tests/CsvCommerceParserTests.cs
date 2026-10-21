using System.Text;
using IRoute.Domain.Exceptions;
using IRoute.Infrastructure.Csv;

namespace IRoute.Tests;

public sealed class CsvCommerceParserTests
{
    private readonly CsvCommerceParser _parser = new();

    [Fact]
    public async Task ParseAsync_ValidCsv_ReturnsRecords()
    {
        const string csv =
            "pc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            "Comercio Uno,12345678,2026-11-02\r\n" +
            "\"Comercio, Dos\",87654321,2026-11-03\r\n";

        var result = await _parser.ParseAsync(ToStream(csv), CancellationToken.None);

        Assert.Equal(2, result.Records.Count);
        Assert.Equal("Comercio Uno", result.Records[0].MerchantName);
        Assert.Equal("12345678", result.Records[0].DocumentNumber);
        Assert.Equal(new DateOnly(2026, 11, 2), result.Records[0].ProcessDate);
        Assert.Equal("Comercio, Dos", result.Records[1].MerchantName);
    }

    [Fact]
    public async Task ParseAsync_SemicolonDelimiter_ReturnsRecords()
    {
        const string csv =
            "pc_nomcomred;pc_numdoc;pc_processdate\r\n" +
            "Comercio Uno;12345678;2026-11-04\r\n";

        var result = await _parser.ParseAsync(ToStream(csv), CancellationToken.None);

        Assert.Single(result.Records);
        Assert.Equal(new DateOnly(2026, 11, 4), result.Records[0].ProcessDate);
    }

    [Fact]
    public async Task ParseAsync_TimestampDate_NormalizesToDate()
    {
        const string csv =
            "pc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            "Comercio Uno,12345678,2026-11-05 00:00:00\r\n";

        var result = await _parser.ParseAsync(ToStream(csv), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 11, 5), Assert.Single(result.Records).ProcessDate);
    }

    [Fact]
    public async Task ParseAsync_Utf8Bom_ReturnsRecords()
    {
        const string csv =
            "\uFEFFpc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            "Comercio Uno,12345678,2026-11-06\r\n";

        var result = await _parser.ParseAsync(ToStream(csv), CancellationToken.None);

        Assert.Single(result.Records);
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsEmpty()
    {
        var result = await _parser.ParseAsync(ToStream(string.Empty), CancellationToken.None);

        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task ParseAsync_HeadersOnly_ReturnsEmpty()
    {
        const string csv = "pc_nomcomred,pc_numdoc,pc_processdate\r\n";

        var result = await _parser.ParseAsync(ToStream(csv), CancellationToken.None);

        Assert.Empty(result.Records);
    }

    [Theory]
    [InlineData("pc_numdoc,pc_processdate", "pc_nomcomred")]
    [InlineData("pc_nomcomred,pc_processdate", "pc_numdoc")]
    [InlineData("pc_nomcomred,pc_numdoc", "pc_processdate")]
    public async Task ParseAsync_MissingRequiredHeader_Throws(string headers, string missing)
    {
        var csv = $"{headers}\r\nComercio Uno,12345678,2026-11-02\r\n";

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _parser.ParseAsync(ToStream(csv), CancellationToken.None));

        Assert.Contains(missing, exception.Message);
    }

    [Fact]
    public async Task ParseAsync_InvalidDate_Throws()
    {
        const string csv =
            "pc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            "Comercio Uno,12345678,2026-13-40\r\n";

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _parser.ParseAsync(ToStream(csv), CancellationToken.None));

        Assert.Contains("2026-13-40", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_DuplicateHeaders_Throws()
    {
        const string csv =
            "pc_nomcomred,pc_numdoc,pc_processdate,pc_numdoc\r\n" +
            "Comercio Uno,12345678,2026-11-02,87654321\r\n";

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _parser.ParseAsync(ToStream(csv), CancellationToken.None));

        Assert.Contains("columnas duplicadas", exception.Message);
        Assert.Contains("pc_numdoc", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_LongMerchantName_Throws()
    {
        var name = new string('A', 201);
        var csv =
            "pc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            $"{name},12345678,2026-11-02\r\n";

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _parser.ParseAsync(ToStream(csv), CancellationToken.None));

        Assert.Contains("nombre de comercio", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_LongDocumentNumber_Throws()
    {
        var document = new string('1', 51);
        var csv =
            "pc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            $"Comercio Uno,{document},2026-11-02\r\n";

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _parser.ParseAsync(ToStream(csv), CancellationToken.None));

        Assert.Contains("número de documento", exception.Message);
    }

    [Fact]
    public async Task ParseAsync_MalformedCsv_Throws()
    {
        const string csv =
            "pc_nomcomred,pc_numdoc,pc_processdate\r\n" +
            "Comercio \"Roto,30000008,2026-11-03\r\n";

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _parser.ParseAsync(ToStream(csv), CancellationToken.None));

        Assert.Contains("datos inválidos", exception.Message);
    }

    private static MemoryStream ToStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));
}
