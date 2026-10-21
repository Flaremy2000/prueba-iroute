using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using IRoute.Application.Abstractions;
using IRoute.Application.Common;
using IRoute.Domain.Entities;
using IRoute.Domain.Exceptions;

namespace IRoute.Infrastructure.Csv;

public sealed class CsvCommerceParser : ICsvCommerceParser
{
    private const int MaxMerchantNameLength = 200;
    private const int MaxDocumentNumberLength = 50;

    private static readonly string[] RequiredHeaders =
    [
        "pc_nomcomred",
        "pc_numdoc",
        "pc_processdate"
    ];

    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "ddMMyyyy",
        "yyyy/MM/dd",
        "yyyy-MM-dd HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss"
    ];

    public async Task<CsvParseResult> ParseAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var hashingStream = new HashingStream(content);
        using var reader = new StreamReader(
            hashingStream,
            encoding: System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: true);

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            DetectDelimiter = true,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            BadDataFound = args => throw new DomainValidationException(
                $"La fila {args.Context.Parser?.Row ?? 0} tiene datos inválidos.")
        };

        using var csv = new CsvReader(reader, configuration);

        if (!await csv.ReadAsync())
        {
            return new CsvParseResult([], hashingStream.GetHashHex());
        }

        csv.ReadHeader();

        var headers = csv.HeaderRecord?
            .Select(header => header.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var duplicateHeaders = csv.HeaderRecord?
            .Select(header => header.Trim())
            .GroupBy(header => header, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray() ?? [];

        if (duplicateHeaders.Length > 0)
        {
            throw new DomainValidationException(
                $"El archivo contiene columnas duplicadas: {string.Join(", ", duplicateHeaders)}.");
        }

        var missingHeaders = RequiredHeaders
            .Where(header => !headers.Contains(header))
            .ToArray();

        if (missingHeaders.Length > 0)
        {
            throw new DomainValidationException(
                $"El archivo no contiene las columnas obligatorias: {string.Join(", ", missingHeaders)}.");
        }

        var records = new List<CommerceRecord>();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = csv.Context.Parser?.Row ?? 0;
            var processDateValue = csv.GetField("pc_processdate");
            var merchantName = csv.GetField("pc_nomcomred");
            var documentNumber = csv.GetField("pc_numdoc");

            if (!TryParseDate(processDateValue, out var processDate))
            {
                throw new DomainValidationException(
                    $"La fila {row} tiene una fecha de proceso inválida: '{processDateValue}'.");
            }

            if (merchantName is { Length: > MaxMerchantNameLength })
            {
                throw new DomainValidationException(
                    $"La fila {row} tiene un nombre de comercio mayor a {MaxMerchantNameLength} caracteres.");
            }

            if (documentNumber is { Length: > MaxDocumentNumberLength })
            {
                throw new DomainValidationException(
                    $"La fila {row} tiene un número de documento mayor a {MaxDocumentNumberLength} caracteres.");
            }

            records.Add(new CommerceRecord
            {
                MerchantName = merchantName,
                DocumentNumber = documentNumber,
                ProcessDate = processDate
            });
        }

        return new CsvParseResult(records, hashingStream.GetHashHex());
    }

    private static bool TryParseDate(string? value, out DateOnly processDate)
    {
        processDate = default;

        if (string.IsNullOrWhiteSpace(value) ||
            !DateTime.TryParseExact(
                value.Trim(),
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateTime))
        {
            return false;
        }

        processDate = DateOnly.FromDateTime(dateTime);
        return true;
    }
}
