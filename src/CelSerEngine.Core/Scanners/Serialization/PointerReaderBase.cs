using CelSerEngine.Core.Models;
using System.Buffers;

namespace CelSerEngine.Core.Scanners.Serialization;

public abstract class PointerReaderBase<T> : IPointerReader where T : IPointerLayout
{
    public Stream BaseStream => _stream;
    protected readonly Stream _stream;
    protected readonly T _layout;

    public PointerReaderBase(Stream stream, T layout)
    {
        _stream = stream;
        _layout = layout;
    }

    /// <inheritdoc />
    public Pointer Read()
    {
        Span<byte> buffer = stackalloc byte[_layout.EntrySizeInBytes];
        _stream.ReadExactly(buffer);

        return ReadFromBuffer(buffer);
    }

    /// <inheritdoc />
    public void ReadExactly(Span<Pointer> destination, int startIndex)
    {
        _stream.Seek(startIndex * _layout.EntrySizeInBytes, SeekOrigin.Begin);
        var totalBytes = destination.Length * _layout.EntrySizeInBytes;
        var remainingBytes = _stream.Length - _stream.Position;

        if (totalBytes > remainingBytes)
            throw new EndOfStreamException($"Not enough bytes in stream: required {totalBytes}, remaining {remainingBytes}");

        const int StackThreshold = 4096; // 4 KB
        var useStackArray = totalBytes <= StackThreshold;
        byte[]? rentedArray = null;

        if (!useStackArray)
        {
            rentedArray = ArrayPool<byte>.Shared.Rent(totalBytes);
        }

        Span<byte> buffer = useStackArray
            ? stackalloc byte[totalBytes]
            : rentedArray.AsSpan(0, totalBytes);

        try
        {
            _stream.ReadExactly(buffer);
            var bufferIndex = 0;

            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = ReadFromBuffer(buffer[bufferIndex..]);
                bufferIndex += _layout.EntrySizeInBytes;
            }

        }
        finally
        {
            if (rentedArray != null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }
    }

    protected abstract Pointer ReadFromBuffer(Span<byte> buffer);

    public void Dispose()
    {
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
