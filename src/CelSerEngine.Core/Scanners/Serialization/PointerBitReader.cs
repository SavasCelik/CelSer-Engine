using CelSerEngine.Core.Models;
using System.Runtime.CompilerServices;

namespace CelSerEngine.Core.Scanners.Serialization;

public class PointerBitReader : PointerReaderBase<PointerBitLayout>
{
    private readonly IReadOnlyList<ModuleInfo> _modules;

    public PointerBitReader(Stream stream, PointerBitLayout pointerBitLayout, IReadOnlyList<ModuleInfo> modules)
        : base(stream, pointerBitLayout)
    {
        _modules = modules;
    }

    protected override Pointer ReadFromBuffer(Span<byte> buffer)
    {
        var bitPos = 0;

        var moduleOffsetMagnitude = ReadBitsUInt(buffer, _layout.MaxBitCountModuleBaseOffset, _layout.MaskModuleBaseOffset, ref bitPos);
        var moduleOffsetSign = ReadBitsInt(buffer, PointerBitLayout.SignBitCount, PointerBitLayout.SignMask, ref bitPos);
        var moduleIndex = ReadBitsInt(buffer, _layout.MaxBitCountModuleIndex, _layout.MaskModuleIndex, ref bitPos);
        var level = ReadBitsInt(buffer, _layout.MaxBitCountLevel, _layout.MaskLevel, ref bitPos);
        var offsets = new IntPtr[level];

        for (var i = 0; i < offsets.Length; i++)
        {
            offsets[i] = ReadBitsInt(buffer, _layout.MaxBitCountOffset, _layout.MaskOffset, ref bitPos);
        }

        long moduleOffset = moduleOffsetSign == 1
            ? -moduleOffsetMagnitude
            : moduleOffsetMagnitude;
        var module = _modules[moduleIndex];

        return new Pointer
        {
            ModuleName = module.Name,
            BaseAddress = module.BaseAddress,
            BaseOffset = (int)moduleOffset,
            Offsets = offsets
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadBitsInt(Span<byte> buffer, int reservedBitCount, int mask, ref int bitPos) =>
        (int)ReadBitsUInt(buffer, reservedBitCount, (uint)mask, ref bitPos);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadBitsUInt(Span<byte> buffer, int reservedBitCount, uint mask, ref int bitPos)
    {
        int byteIndex = bitPos >> 3; // the current byte that still has free bits available
        int bitOffset = bitPos & 7; // at which bit of the current byte are we?

        var value = (Unsafe.As<byte, uint>(ref buffer[byteIndex]) >> bitOffset) & mask;
        bitPos += reservedBitCount;

        return value;
    }
}
