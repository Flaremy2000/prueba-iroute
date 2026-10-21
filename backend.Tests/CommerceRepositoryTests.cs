using IRoute.Application.Quarantine;
using IRoute.Domain.Entities;
using IRoute.Infrastructure.Persistence;
using IRoute.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IRoute.Tests;

public sealed class CommerceRepositoryTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("IROUTE_TEST_DB")
        ?? @"Server=.\SQLEXPRESS;Database=IRouteCommerce;Trusted_Connection=True;TrustServerCertificate=True;";

    private static bool DbTestsEnabled =>
        Environment.GetEnvironmentVariable("IROUTE_DB_TESTS") == "1";

    [Fact]
    public async Task CreateAndProcess_RoundTrip()
    {
        if (!DbTestsEnabled)
        {
            return;
        }

        await using var db = CreateContext();
        var date = new DateOnly(2099, 1, 2);
        await CleanupAsync(db, date);

        try
        {
            var repository = new CommerceRepository(db);
            var records = new[]
            {
                new CommerceRecord
                {
                    MerchantName = "Comercio Uno",
                    DocumentNumber = "12345678",
                    ProcessDate = date
                },
                new CommerceRecord
                {
                    MerchantName = "Comercio Dos",
                    DocumentNumber = string.Empty,
                    ProcessDate = date
                },
                new CommerceRecord
                {
                    MerchantName = "Comercio Tres",
                    DocumentNumber = "AB123",
                    ProcessDate = date
                }
            };

            var inserted = await repository.CreateAsync(records, CancellationToken.None);
            Assert.Equal(3, inserted);

            var counts = await repository.ProcessAsync(date, CancellationToken.None);
            Assert.Equal(2, counts.Quarantined);
            Assert.Equal(1, counts.Remaining);

            var queryRepository = new QuarantineRepository(db);
            var page = await queryRepository.GetAsync(
                new GetQuarantineQuery(date, null, 1, 10),
                CancellationToken.None);

            Assert.Equal(2, page.Total);
            Assert.Equal(2, page.Items.Count);
            Assert.All(page.Items, item => Assert.Equal(date, item.ProcessDate));
        }
        finally
        {
            await CleanupAsync(db, date);
        }
    }

    [Fact]
    public async Task ProcessAsync_WithoutData_ReturnsZero()
    {
        if (!DbTestsEnabled)
        {
            return;
        }

        await using var db = CreateContext();
        var date = new DateOnly(2099, 1, 3);
        await CleanupAsync(db, date);

        var repository = new CommerceRepository(db);
        var counts = await repository.ProcessAsync(date, CancellationToken.None);

        Assert.Equal(0, counts.Quarantined);
        Assert.Equal(0, counts.Remaining);
    }

    private static CommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new CommerceDbContext(options);
    }

    private static async Task CleanupAsync(CommerceDbContext db, DateOnly date)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dbo.commerce WHERE pc_processdate = {date}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dbo.commerce_quarantine WHERE pc_processdate = {date}");
    }
}
