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
            var foundMatchingSegments = FindMatchingMemorySegmentsInRegion(virtualMemoryRegion);
            matchingProcessMemories.AddRange(foundMatchingSegments);

            if (progressBarUpdater != null)
            {
                var progress = (float)regionIndex * 100 / virtualMemoryRegions.Count;
                progressBarUpdater.Report(progress);
            }
        }

        return matchingProcessMemories;
    }

    private IEnumerable<IMemorySegment> FindMatchingMemorySegmentsInRegion(VirtualMemoryRegion virtualMemoryRegion)
    {
        var remaining = (int)virtualMemoryRegion.RegionSize % Vector<byte>.Count;
        var regionBytes = virtualMemoryRegion.Bytes;

        for (var i = 0; i < (int)virtualMemoryRegion.RegionSize - remaining; i += Vector<byte>.Count)
        {
            var splitBuffer = regionBytes.AsSpan().Slice(i, Vector<byte>.Count);
            var compareResult = CompareTo(splitBuffer);

            foreach (var vectorIndex in FindMatchingVectorIndexes(compareResult))
            {
                var offset = i + vectorIndex;
                var memoryValue = regionBytes.AsSpan().Slice(offset, _sizeOfT).ConvertToString(_scanConstraint.ScanDataType);
                yield return new MemorySegment(virtualMemoryRegion.BaseAddress, offset, memoryValue, _scanConstraint.ScanDataType);
            }
        }
    }

    private IEnumerable<int> FindMatchingVectorIndexes(Vector<byte> vectorResult)
    {
        if (vectorResult.Equals(Vector<byte>.Zero))
            yield break;

        for (var j = 0; j < Vector<byte>.Count; j += _sizeOfT)
        {
            if (vectorResult[j] != 0)
                yield return j;
        }
    }
}
