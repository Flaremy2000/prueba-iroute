using IRoute.Application.Abstractions;
using IRoute.Application.Commerce;
using IRoute.Application.Common;
using IRoute.Domain.Entities;
using IRoute.Domain.Exceptions;

namespace IRoute.Tests;

public sealed class CommerceCommandServiceTests
{
    private const string ContentHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private readonly FakeCsvParser _parser = new();
    private readonly FakeCommerceRepository _repository = new();

    [Fact]
    public async Task UploadAsync_InvalidFileName_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream([1]), "comercio_01112026.csv"),
                CancellationToken.None));

        Assert.Contains("commerce_DDMMYYYY.csv", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_InvalidDateInFileName_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream([1]), "commerce_32132026.csv"),
                CancellationToken.None));

        Assert.Contains("fecha válida", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_EmptyFileName_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream([1]), " "),
                CancellationToken.None));

        Assert.Contains("nombre del archivo", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_UnreadableContent_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new UnreadableStream(), "commerce_01112026.csv"),
                CancellationToken.None));

        Assert.Contains("no se puede leer", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_EmptyContent_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream(), "commerce_01112026.csv"),
                CancellationToken.None));

        Assert.Contains("vacío", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_NoRecords_Throws()
    {
        var service = CreateService();
        _parser.Result = new CsvParseResult([], ContentHash);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream([1]), "commerce_01112026.csv"),
                CancellationToken.None));

        Assert.Contains("no contiene registros", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_DateMismatch_Throws()
    {
        var service = CreateService();
        _parser.Result = new CsvParseResult(
        [
            new CommerceRecord
            {
                MerchantName = "Comercio Uno",
                DocumentNumber = "12345678",
                ProcessDate = new DateOnly(2026, 11, 3)
            }
        ], ContentHash);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream([1]), "commerce_01112026.csv"),
                CancellationToken.None));

        Assert.Contains("2026-11-03", exception.Message);
        Assert.Contains("2026-11-01", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_Valid_DelegatesToRepositoryAndReturnsResult()
    {
        var service = CreateService();
        _parser.Result = new CsvParseResult(
        [
            new CommerceRecord
            {
                MerchantName = "Comercio Uno",
                DocumentNumber = "12345678",
                ProcessDate = new DateOnly(2026, 11, 1)
            }
        ], ContentHash);
        _repository.Inserted = 1;

        var result = await service.UploadAsync(
            new UploadCommerceCommand(new MemoryStream([1]), @"C:\tmp\commerce_01112026.csv"),
            CancellationToken.None);

        Assert.Equal("commerce_01112026.csv", result.FileName);
        Assert.Equal(new DateOnly(2026, 11, 1), result.ProcessDate);
        Assert.Equal(1, result.RowsRead);
        Assert.Equal(1, result.RowsInserted);
        Assert.NotNull(_repository.LastRecords);
        Assert.Single(_repository.LastRecords);
        Assert.Equal("commerce_01112026.csv", _repository.LastFileName);
        Assert.Equal(ContentHash, _repository.LastContentHash);
        Assert.Equal(new DateOnly(2026, 11, 1), _repository.LastUploadProcessDate);
    }

    [Fact]
    public async Task UploadAsync_DuplicateFile_ThrowsConflict()
    {
        var service = CreateService();
        _parser.Result = new CsvParseResult(
        [
            new CommerceRecord
            {
                MerchantName = "Comercio Uno",
                DocumentNumber = "12345678",
                ProcessDate = new DateOnly(2026, 11, 1)
            }
        ], ContentHash);
        _repository.Duplicate = true;

        var exception = await Assert.ThrowsAsync<DomainConflictException>(
            () => service.UploadAsync(
                new UploadCommerceCommand(new MemoryStream([1]), "commerce_01112026.csv"),
                CancellationToken.None));

        Assert.Contains("ya fue cargado", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_DefaultDate_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.ProcessAsync(
                new ProcessCommerceCommand(default),
                CancellationToken.None));

        Assert.Contains("fecha de proceso", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_Valid_ReturnsRepositoryCounts()
    {
        var service = CreateService();
        _repository.Counts = new ProcessCounts(2, 3);

        var result = await service.ProcessAsync(
            new ProcessCommerceCommand(new DateOnly(2026, 11, 2)),
            CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 11, 2), result.ProcessDate);
        Assert.Equal(2, result.Quarantined);
        Assert.Equal(3, result.Remaining);
        Assert.Equal(new DateOnly(2026, 11, 2), _repository.LastProcessDate);
    }

    private CommerceCommandService CreateService() => new(_parser, _repository);

    private sealed class FakeCsvParser : ICsvCommerceParser
    {
        public CsvParseResult Result { get; set; } = new([], ContentHash);

        public Task<CsvParseResult> ParseAsync(Stream content, CancellationToken cancellationToken) =>
            Task.FromResult(Result);
    }

    private sealed class FakeCommerceRepository : ICommerceRepository
    {
        public int Inserted { get; set; }
        public bool Duplicate { get; set; }
        public ProcessCounts Counts { get; set; } = new(0, 0);
        public IReadOnlyCollection<CommerceRecord>? LastRecords { get; private set; }
        public string? LastFileName { get; private set; }
        public string? LastContentHash { get; private set; }
        public DateOnly? LastUploadProcessDate { get; private set; }
        public DateOnly? LastProcessDate { get; private set; }

        public Task<UploadInsertResult> CreateAsync(
            IReadOnlyCollection<CommerceRecord> records,
            string fileName,
            string contentHash,
            DateOnly processDate,
            CancellationToken cancellationToken)
        {
            LastRecords = records;
            LastFileName = fileName;
            LastContentHash = contentHash;
            LastUploadProcessDate = processDate;

            return Task.FromResult(new UploadInsertResult(Inserted, Duplicate));
        }

        public Task<ProcessCounts> ProcessAsync(
            DateOnly processDate,
            CancellationToken cancellationToken)
        {
            LastProcessDate = processDate;
            return Task.FromResult(Counts);
        }
    }

    private sealed class UnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 1;
        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
