using IRoute.Application.Abstractions;
using IRoute.Domain.Exceptions;

namespace IRoute.Application.Quarantine;

public sealed class QuarantineQueryService(
    IQuarantineRepository repository) : IQuarantineQueryService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public Task<PagedResult<QuarantineItem>> GetAsync(
        GetQuarantineQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            throw new DomainValidationException("La página debe ser mayor o igual a 1.");
        }

        var pageSize = query.PageSize == 0 ? DefaultPageSize : query.PageSize;

        if (pageSize is < 1 or > MaxPageSize)
        {
            throw new DomainValidationException(
                $"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
        }

        return repository.GetAsync(
            query with { PageSize = pageSize },
            cancellationToken);
    }
}
