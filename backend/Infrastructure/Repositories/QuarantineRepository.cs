using IRoute.Application.Abstractions;
using IRoute.Application.Quarantine;
using IRoute.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IRoute.Infrastructure.Repositories;

public sealed class QuarantineRepository(
    CommerceDbContext dbContext) : IQuarantineRepository
{
    public async Task<PagedResult<QuarantineItem>> GetAsync(
        GetQuarantineQuery query,
        CancellationToken cancellationToken)
    {
        var source = dbContext.Quarantine.AsNoTracking();

        if (query.ProcessDate.HasValue)
        {
            source = source.Where(item => item.ProcessDate == query.ProcessDate);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            source = source.Where(item =>
                (item.MerchantName != null && item.MerchantName.Contains(search)) ||
                (item.DocumentNumber != null && item.DocumentNumber.Contains(search)));
        }

        var total = await source.LongCountAsync(cancellationToken);

        var skip = (int)Math.Min(
            ((long)query.Page - 1) * query.PageSize,
            int.MaxValue);

        var rows = await source
            .OrderByDescending(item => item.Id)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(item => new
            {
                item.Id,
                item.MerchantName,
                item.DocumentNumber,
                item.ProcessDate,
                item.Motivo
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new QuarantineItem(
                row.Id,
                row.MerchantName,
                row.DocumentNumber,
                row.ProcessDate,
                row.Motivo))
            .ToArray();

        return new PagedResult<QuarantineItem>(
            items,
            query.Page,
            query.PageSize,
            total);
    }
}
