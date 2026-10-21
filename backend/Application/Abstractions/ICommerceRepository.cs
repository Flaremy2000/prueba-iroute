using IRoute.Application.Commerce;
using IRoute.Domain.Entities;

namespace IRoute.Application.Abstractions;

public interface ICommerceRepository
{
    Task<UploadInsertResult> CreateAsync(
        IReadOnlyCollection<CommerceRecord> records,
        string fileName,
        string contentHash,
        DateOnly processDate,
        CancellationToken cancellationToken);

    Task<ProcessCounts> ProcessAsync(
        DateOnly processDate,
        CancellationToken cancellationToken);
}
