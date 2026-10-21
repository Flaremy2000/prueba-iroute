using IRoute.Domain.Entities;

namespace IRoute.Application.Common;

public sealed record CsvParseResult(IReadOnlyList<CommerceRecord> Records);
