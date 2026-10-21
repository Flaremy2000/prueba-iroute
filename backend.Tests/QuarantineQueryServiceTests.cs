using IRoute.Application.Abstractions;
using IRoute.Application.Quarantine;
using IRoute.Domain.Exceptions;

namespace IRoute.Tests;

public sealed class QuarantineQueryServiceTests
{
    private readonly FakeQuarantineRepository _repository = new();

    [Fact]
    public async Task GetAsync_PageLessThanOne_Throws()
    {
        var service = new QuarantineQueryService(_repository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetAsync(
                new GetQuarantineQuery(null, null, 0, 10),
                CancellationToken.None));

        Assert.Contains("página", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetAsync_PageSizeOutOfRange_Throws(int pageSize)
    {
        var service = new QuarantineQueryService(_repository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetAsync(
                new GetQuarantineQuery(null, null, 1, pageSize),
                CancellationToken.None));

        Assert.Contains("tamaño de página", exception.Message);
    }

    [Fact]
    public async Task GetAsync_PageSizeZero_UsesDefault()
    {
        var service = new QuarantineQueryService(_repository);

        await service.GetAsync(
            new GetQuarantineQuery(null, null, 1, 0),
            CancellationToken.None);

        Assert.NotNull(_repository.LastQuery);
        Assert.Equal(20, _repository.LastQuery.PageSize);
    }

    [Fact]
    public async Task GetAsync_Valid_DelegatesToRepository()
    {
        var service = new QuarantineQueryService(_repository);
        var expected = new PagedResult<QuarantineItem>([], 2, 5, 0);
        _repository.Result = expected;

        var result = await service.GetAsync(
            new GetQuarantineQuery(new DateOnly(2026, 11, 2), "Comercio", 2, 5),
            CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(new DateOnly(2026, 11, 2), _repository.LastQuery?.ProcessDate);
        Assert.Equal("Comercio", _repository.LastQuery?.Search);
        Assert.Equal(2, _repository.LastQuery?.Page);
        Assert.Equal(5, _repository.LastQuery?.PageSize);
    }

    private sealed class FakeQuarantineRepository : IQuarantineRepository
    {
        public PagedResult<QuarantineItem> Result { get; set; } = new([], 1, 20, 0);
        public GetQuarantineQuery? LastQuery { get; private set; }

        public Task<PagedResult<QuarantineItem>> GetAsync(
            GetQuarantineQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(Result);
        }
    }
}
