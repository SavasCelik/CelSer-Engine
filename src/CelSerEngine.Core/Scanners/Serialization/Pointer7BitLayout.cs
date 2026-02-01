using System.Diagnostics;
using System.Numerics;

namespace CelSerEngine.Core.Scanners.Serialization;

/// <summary>
/// Information about pointer's 7bit encoded layout when serializing
/// </summary>
public sealed class Pointer7BitLayout : IPointerLayout
{
    public int MaxByteCountModuleIndex { get; }
    public int MaxByteCountModuleBaseOffset { get; }
    public int MaxByteCountCountLevel { get; }
    public int MaxByteCountCountOffset { get; }
    public int EntrySizeInBytes { get; init; }

    public Pointer7BitLayout(
        int maxModuleIndex,
        uint maxModuleOffset,
        int maxLevel,
        int maxOffset)
    {
        Debug.Assert(maxModuleIndex > 0);
        Debug.Assert(maxLevel > 0);
        Debug.Assert(maxOffset > 0);

        // the exact size of an entry cannot be determined in advance
        // therefore, we calculate the "worst-case size" for each component
        // and give every entry that fixed size
        var maxByteCountMaxModuleIndex = Get7BitEncodedIntSize(maxModuleIndex);
        var maxByteCountMaxModuleOffset = 10;
        var maxByteCountMaxLevel = Get7BitEncodedIntSize(maxLevel);
        var maxByteCountMaxOffset = Get7BitEncodedIntSize(maxOffset);
        EntrySizeInBytes = maxByteCountMaxModuleIndex + maxByteCountMaxModuleOffset + maxByteCountMaxLevel + maxByteCountMaxOffset * maxLevel;
    }

    private static int Get7BitEncodedIntSize(int value) =>
        BitOperations.Log2((uint)value) / 7 + 1;
}
