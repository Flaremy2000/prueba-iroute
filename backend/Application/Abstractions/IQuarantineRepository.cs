using IRoute.Application.Quarantine;

namespace IRoute.Application.Abstractions;

public interface IQuarantineRepository
{
    Task<PagedResult<QuarantineItem>> GetAsync(
        GetQuarantineQuery query,
        CancellationToken cancellationToken);
}
