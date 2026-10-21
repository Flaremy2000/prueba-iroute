using IRoute.Application.Commerce;

namespace IRoute.Application.Abstractions;

public interface ICommerceCommandService
{
    Task<UploadCommerceResult> UploadAsync(
        UploadCommerceCommand command,
        CancellationToken cancellationToken);

    Task<ProcessCommerceResult> ProcessAsync(
        ProcessCommerceCommand command,
        CancellationToken cancellationToken);
}
