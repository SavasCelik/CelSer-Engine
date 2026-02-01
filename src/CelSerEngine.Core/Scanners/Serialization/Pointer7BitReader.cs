using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Scanners.Serialization;

public class Pointer7BitReader : PointerReaderBase<Pointer7BitLayout>
{
    private readonly IReadOnlyList<ModuleInfo> _modules;

    public Pointer7BitReader(Stream stream, Pointer7BitLayout layout, IReadOnlyList<ModuleInfo> modules)
        : base(stream, layout)
    {
        _modules = modules;
    }

    protected override Pointer ReadFromBuffer(Span<byte> buffer)
    {
        var bufferIndex = 0;
        var moduleOffset = Read7BitEncodedInt64(buffer, ref bufferIndex);
        var moduleIndex = Read7BitEncodedInt(buffer, ref bufferIndex);
        var level = Read7BitEncodedInt(buffer, ref bufferIndex);
        var offsets = new IntPtr[level];

        for (var i = 0; i < level; i++)
        {
            offsets[i] = new IntPtr(Read7BitEncodedInt(buffer, ref bufferIndex));
        }

        var module = _modules[moduleIndex];

        return new Pointer
        {
            ModuleName = module.Name,
            BaseAddress = module.BaseAddress,
            BaseOffset = (int)moduleOffset,
            Offsets = offsets
        };
    }

    /// Inspired from <inheritdoc cref="BinaryReader.Read7BitEncodedInt"/>
    private static int Read7BitEncodedInt(ReadOnlySpan<byte> buffer, ref int byteIndex)
    {
        // Unlike writing, we can't delegate to the 64-bit read on
        // 64-bit platforms. The reason for this is that we want to
        // stop consuming bytes if we encounter an integer overflow.

        uint result = 0;
        byte byteReadJustNow;

        // Read the integer 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        //
        // There are two failure cases: we've read more than 5 bytes,
        // or the fifth byte is about to cause integer overflow.
        // This means that we can read the first 4 bytes without
        // worrying about integer overflow.

        const int MaxBytesWithoutOverflow = 4;
        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            // ReadByte handles end of stream cases for us.
            byteReadJustNow = buffer[byteIndex++];
            result |= (byteReadJustNow & 0x7Fu) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (int)result; // early exit
            }
        }

        // Read the 5th byte. Since we already read 28 bits,
        // the value of this byte must fit within 4 bits (32 - 28),
        // and it must not have the high bit set.

        byteReadJustNow = buffer[byteIndex++];
        if (byteReadJustNow > 0b_1111u)
        {
            throw new FormatException("Bad 7Bit Format");
        }

        result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    /// Inspired from <inheritdoc cref="BinaryReader.Read7BitEncodedInt64"/>
    private static long Read7BitEncodedInt64(ReadOnlySpan<byte> buffer, ref int byteIndex)
    {
        ulong result = 0;
        byte byteReadJustNow;

        // Read the integer 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        //
        // There are two failure cases: we've read more than 10 bytes,
        // or the tenth byte is about to cause integer overflow.
        // This means that we can read the first 9 bytes without
        // worrying about integer overflow.

        const int MaxBytesWithoutOverflow = 9;
        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            // ReadByte handles end of stream cases for us.
            byteReadJustNow = buffer[byteIndex++];
            result |= (byteReadJustNow & 0x7Ful) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (long)result; // early exit
            }
        }

        // Read the 10th byte. Since we already read 63 bits,
        // the value of this byte must fit within 1 bit (64 - 63),
        // and it must not have the high bit set.

        byteReadJustNow = buffer[byteIndex++];
        if (byteReadJustNow > 0b_1u)
        {
            throw new FormatException("Bad 7Bit Format");
        }

        result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (long)result;
    }
}
