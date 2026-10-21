using System.Globalization;
using System.Text.RegularExpressions;
using IRoute.Application.Abstractions;
using IRoute.Domain.Exceptions;

namespace IRoute.Application.Commerce;

public sealed partial class CommerceCommandService(
    ICsvCommerceParser parser,
    ICommerceRepository repository) : ICommerceCommandService
{
    public async Task<UploadCommerceResult> UploadAsync(
        UploadCommerceCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new DomainValidationException("El nombre del archivo es obligatorio.");
        }

        var fileName = Path.GetFileName(command.FileName);
        var processDate = ParseProcessDate(fileName);

        if (command.Content is null || !command.Content.CanRead)
        {
            throw new DomainValidationException("El archivo no se puede leer.");
        }

        if (command.Content.CanSeek && command.Content.Length == 0)
        {
            throw new DomainValidationException("El archivo está vacío.");
        }

        var parseResult = await parser.ParseAsync(command.Content, cancellationToken);

        if (parseResult.Records.Count == 0)
        {
            throw new DomainValidationException("El archivo no contiene registros.");
        }

        var mismatchedRecord = parseResult.Records
            .FirstOrDefault(record => record.ProcessDate != processDate);

        if (mismatchedRecord is not null)
        {
            throw new DomainValidationException(
                $"El archivo contiene registros con fecha {mismatchedRecord.ProcessDate:yyyy-MM-dd}, " +
                $"pero el nombre indica {processDate:yyyy-MM-dd}.");
        }

        var insert = await repository.CreateAsync(
            parseResult.Records,
            fileName,
            parseResult.ContentHash,
            processDate,
            cancellationToken);

        if (insert.Duplicate)
        {
            throw new DomainConflictException(
                "El archivo ya fue cargado. No se insertaron registros duplicados.");
        }

        return new UploadCommerceResult(
            fileName,
            processDate,
            parseResult.Records.Count,
            insert.Inserted);
    }

    public async Task<ProcessCommerceResult> ProcessAsync(
        ProcessCommerceCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ProcessDate == default)
        {
            throw new DomainValidationException("La fecha de proceso es obligatoria.");
        }

        var counts = await repository.ProcessAsync(command.ProcessDate, cancellationToken);

        return new ProcessCommerceResult(
            command.ProcessDate,
            counts.Quarantined,
            counts.Remaining);
    }

    private static DateOnly ParseProcessDate(string fileName)
    {
        var match = FileNamePattern().Match(fileName);

        if (!match.Success ||
            !DateOnly.TryParseExact(
                match.Groups["date"].Value,
                "ddMMyyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var processDate))
        {
            throw new DomainValidationException(
                "El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida.");
        }

        return processDate;
    }

    [GeneratedRegex(@"^commerce_(?<date>\d{8})\.csv$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();
}
