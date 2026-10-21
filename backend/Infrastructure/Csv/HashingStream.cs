using System.Security.Cryptography;

namespace IRoute.Infrastructure.Csv;

internal sealed class HashingStream(Stream inner) : Stream
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => inner.CanWrite;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public string GetHashHex() => Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();

    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);

        if (read > 0)
        {
            _hash.AppendData(buffer, offset, read);
        }

        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = inner.Read(buffer);

        if (read > 0)
        {
            _hash.AppendData(buffer[..read]);
        }

        return read;
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        var read = await inner.ReadAsync(buffer, offset, count, cancellationToken);

        if (read > 0)
        {
            _hash.AppendData(buffer, offset, read);
        }

        return read;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken);

        if (read > 0)
        {
            _hash.AppendData(buffer.Span[..read]);
        }

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void SetLength(long value) => inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) =>
        inner.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        inner.WriteAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hash.Dispose();
        }

        base.Dispose(disposing);
    }
}
