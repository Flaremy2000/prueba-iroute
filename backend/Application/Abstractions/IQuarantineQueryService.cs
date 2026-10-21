using IRoute.Application.Quarantine;

namespace IRoute.Application.Abstractions;

public interface IQuarantineQueryService
{
    Task<PagedResult<QuarantineItem>> GetAsync(
        GetQuarantineQuery query,
        CancellationToken cancellationToken);
}
