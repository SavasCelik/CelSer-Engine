using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CelSerEngine.Core.Scanners.Serialization;

public class PointerBitWriter : IPointerWriter
{
    private readonly Stream _stream;
    private readonly PointerBitLayout _layout;

    public PointerBitWriter(Stream stream, PointerBitLayout pointerBitLayout)
    {
        _stream = stream;
        _layout = pointerBitLayout;
    }

    /// <inheritdoc />
    public void Write(int level, int moduleIndex, long baseOffset, ReadOnlySpan<nint> offsets)
    {
        Span<byte> buffer = stackalloc byte[_layout.EntrySizeInBytes];
        var bitPos = 0;

        // if baseOffset = -100 = 0b11111111111111111111111110011100, we save the magnitude 100 = 0b01100100 and use 1 additional bit to determine the sign
        WriteBits(buffer, Math.Abs(baseOffset), _layout.MaxBitCountModuleBaseOffset, ref bitPos);

        // could have used zigzag encoding instead of manually entering a sign bit, but i figured this keeps things simpler
        int sign = baseOffset < 0 ? 1 : 0;
        WriteBits(buffer, sign, PointerBitLayout.SignBitCount, ref bitPos);

        WriteBits(buffer, moduleIndex, _layout.MaxBitCountModuleIndex, ref bitPos);
        WriteBits(buffer, level + 1, _layout.MaxBitCountLevel, ref bitPos);

        foreach (var tempResult in offsets)
        {
            WriteBits(buffer, tempResult.ToInt32(), _layout.MaxBitCountOffset, ref bitPos);
        }

        _stream.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBits(Span<byte> buffer, long value, int reservedBitCount, ref int bitPos)
    {
        Debug.Assert(value >= 0);
        // bitPos      -> total bits written so far
        // byteIndex   -> bitPos / 8
        // bitOffset   -> bitPos % 8

        int byteIndex = bitPos >> 3; // the current byte that still has free bits available
        int bitOffset = bitPos & 7; // at which bit of the current byte are we?

        // inspired from https://github.com/dotnet/dotnet/blob/release/9.0.1xx/src/runtime/src/libraries/System.Private.CoreLib/src/System/BitConverter.cs#L108
        Unsafe.As<byte, long>(ref buffer[byteIndex]) |= value << bitOffset;
        bitPos += reservedBitCount;
    }

    public void Dispose()
    {
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
