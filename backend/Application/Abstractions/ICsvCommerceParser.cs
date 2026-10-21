using IRoute.Application.Common;

namespace IRoute.Application.Abstractions;

public interface ICsvCommerceParser
{
    Task<CsvParseResult> ParseAsync(
        Stream content,
        CancellationToken cancellationToken);
}
