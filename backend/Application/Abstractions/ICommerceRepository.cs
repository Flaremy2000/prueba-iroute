using IRoute.Domain.Entities;

namespace IRoute.Application.Abstractions;

public interface ICommerceRepository
{
    Task<int> CreateAsync(
        IReadOnlyCollection<CommerceRecord> records,
        CancellationToken cancellationToken);

    Task<ProcessCounts> ProcessAsync(
        DateOnly processDate,
        CancellationToken cancellationToken);
}
