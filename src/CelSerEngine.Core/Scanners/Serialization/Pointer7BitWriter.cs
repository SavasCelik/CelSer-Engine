namespace CelSerEngine.Core.Scanners.Serialization;

public class Pointer7BitWriter : IPointerWriter
{
    private readonly Stream _stream;
    private readonly Pointer7BitLayout _layout;

    public Pointer7BitWriter(Stream stream, Pointer7BitLayout layout)
    {
        _stream = stream;
        _layout = layout;
    }

    /// <inheritdoc />
    public void Write(int level, int moduleIndex, long baseOffset, ReadOnlySpan<nint> offsets)
    {
        Span<byte> buffer = stackalloc byte[_layout.EntrySizeInBytes];
        var bufferIndex = 0;

        bufferIndex += Write7BitEncodedInt64(buffer[bufferIndex..], baseOffset);
        bufferIndex += Write7BitEncodedInt(buffer[bufferIndex..], moduleIndex);
        bufferIndex += Write7BitEncodedInt(buffer[bufferIndex..], level + 1);

        foreach (var tempResult in offsets)
        {
            bufferIndex += Write7BitEncodedInt(buffer[bufferIndex..], tempResult.ToInt32());
        }

        _stream.Write(buffer);
    }

    /// Inspired from <inheritdoc cref="BinaryWriter.Write7BitEncodedInt"/>
    private static int Write7BitEncodedInt(Span<byte> buffer, int value)
    {
        var i = 0;
        uint uValue = (uint)value;

        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        while (uValue > 0x7Fu)
        {
            buffer[i++] = (byte)(uValue | ~0x7Fu);
            uValue >>= 7;
        }

        buffer[i++] = (byte)uValue;

        return i;
    }

    /// Inspired from <inheritdoc cref="BinaryWriter.Write7BitEncodedInt64"/>
    private static int Write7BitEncodedInt64(Span<byte> buffer, long value)
    {
        var i = 0;
        ulong uValue = (ulong)value;

        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        while (uValue > 0x7Fu)
        {
            buffer[i++] = (byte)((uint)uValue | ~0x7Fu);
            uValue >>= 7;
        }

        buffer[i++] = (byte)uValue;

        return i;
    }

    public void Dispose()
    {
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
