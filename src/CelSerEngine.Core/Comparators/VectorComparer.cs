using System.Numerics;
using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public class VectorComparer<T> : IScanComparer where T : struct, INumber<T>
{
    private readonly ScanConstraint _scanConstraint;
    private readonly Vector<T> _userInputAsVector;
    private readonly int _sizeOfT;


    public VectorComparer(ScanConstraint scanConstraint)
    {
        _scanConstraint = scanConstraint;
        _userInputAsVector = new Vector<T>(scanConstraint.UserInput.ParseNumber<T>());
        _sizeOfT = scanConstraint.ScanDataType.GetPrimitiveSize();
    }

    public int GetVectorSize()
    {
        return Vector<T>.Count;
    }

    public Vector<byte> CompareTo(ReadOnlySpan<byte> bytes)
    {
        return _scanConstraint.ScanCompareType switch
        {
            ScanCompareType.ExactValue => Vector.AsVectorByte(Vector.Equals(new Vector<T>(bytes), _userInputAsVector)),
            ScanCompareType.SmallerThan => Vector.AsVectorByte(Vector.LessThan(new Vector<T>(bytes), _userInputAsVector)),
            ScanCompareType.BiggerThan => Vector.AsVectorByte(Vector.GreaterThan(new Vector<T>(bytes), _userInputAsVector)),
            _ => throw new NotImplementedException("Not implemented")
        };
    }

    public IList<IMemorySegment> GetMatchingMemorySegments(IList<VirtualMemoryRegion> virtualMemoryRegions, IProgress<float>? progressBarUpdater = null)
    {
        var matchingProcessMemories = new List<IMemorySegment>();

        for (var regionIndex = 0; regionIndex < virtualMemoryRegions.Count; regionIndex++)
        {
            var virtualMemoryRegion = virtualMemoryRegions[regionIndex];
            // TODO: search the remaining section with a normal for loop
            var remaining = (int)virtualMemoryRegion.RegionSize % Vector<byte>.Count;
            var regionBytesAsSpan = virtualMemoryRegion.Bytes.AsSpan();

            for (var i = 0; i < (int)virtualMemoryRegion.RegionSize - remaining; i += Vector<byte>.Count)
            {
                var splitBuffer = regionBytesAsSpan.Slice(i, Vector<byte>.Count);
                var compareResult = CompareTo(splitBuffer);

                if (!compareResult.Equals(Vector<byte>.Zero))
                {
                    for (var j = 0; j < Vector<byte>.Count; j += _sizeOfT)
                    {
                        if (compareResult[j] != 0)
                        {
                            var offset = i + j;
                            var memoryValue = regionBytesAsSpan.Slice(offset, _sizeOfT).ConvertToString(_scanConstraint.ScanDataType);

                            matchingProcessMemories.Add(
                                new MemorySegment(
                                    virtualMemoryRegion.BaseAddress, offset,
                                    memoryValue,
                                    _scanConstraint.ScanDataType));
                        }
                    }
                }
            }

            var progress = (float)regionIndex * 100 / virtualMemoryRegions.Count;
            progressBarUpdater?.Report(progress);
        }

        return matchingProcessMemories;
    }
}
