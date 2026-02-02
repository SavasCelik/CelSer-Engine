using System.Numerics;

namespace CelSerEngine.Core.Scanners.Serialization;

/// <summary>
/// Information about pointer's bit layout when serializing
/// </summary>
public sealed class PointerBitLayout : IPointerLayout
{
    public int MaxBitCountModuleIndex { get; }
    public int MaxBitCountModuleBaseOffset { get; }
    public int MaxBitCountLevel { get; }
    public int MaxBitCountOffset { get; }

    public int EntrySizeInBytes { get; init; }

    public int MaskModuleIndex { get; }
    public uint MaskModuleBaseOffset { get; }
    public int MaskLevel { get; }
    public int MaskOffset { get; }

    public const int SignBitCount = 1;
    public const int SignMask = 0b1;

    public PointerBitLayout(
        int maxModuleIndex,
        uint maxModuleOffset,
        int maxLevel,
        int maxOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxModuleIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLevel);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxLevel, 30); // no one want that
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOffset);

        // the exact size of an entry cannot be determined in advance
        // therefore, we calculate the "worst-case size" for each component
        // and give every entry that fixed size
        MaxBitCountModuleIndex = BitOperations.Log2((uint)maxModuleIndex) + 1;
        MaxBitCountModuleBaseOffset = BitOperations.Log2(maxModuleOffset) + 1;
        MaxBitCountLevel = BitOperations.Log2((uint)maxLevel) + 1;
        MaxBitCountOffset = BitOperations.Log2((uint)maxOffset) + 1;

        int totalBitCount = checked(
            MaxBitCountModuleIndex +
            MaxBitCountModuleBaseOffset +
            SignBitCount +
            MaxBitCountLevel +
            MaxBitCountOffset * maxLevel);

        // round up the bit count to the nearest whole byte
        EntrySizeInBytes = (totalBitCount + 7) / 8;

        // handle edge case with 32 bit count: only applies to base offset, the other ones are always signed integers
        MaskModuleBaseOffset = MaxBitCountModuleBaseOffset == 32
                ? uint.MaxValue
                : (1u << MaxBitCountModuleBaseOffset) - 1;

        MaskModuleIndex = (1 << MaxBitCountModuleIndex) - 1;
        MaskLevel = (1 << MaxBitCountLevel) - 1;
        MaskOffset = (1 << MaxBitCountOffset) - 1;
    }
}