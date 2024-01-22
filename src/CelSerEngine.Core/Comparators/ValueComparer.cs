using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;
using System.Numerics;

namespace CelSerEngine.Core.Comparators;

public class ValueComparer : IScanComparer
{
    private readonly ScanConstraint _scanConstraint;
    private string _userInput;
    private readonly int _sizeOfT;

    public ValueComparer(ScanConstraint scanConstraint)
    {
        _scanConstraint = scanConstraint;
        _userInput = scanConstraint.UserInput;
        _sizeOfT = scanConstraint.ScanDataType.GetPrimitiveSize();
    }

    public static bool MeetsTheScanConstraint(string lhs, string rhs, ScanConstraint scanConstraint)
    {
        return scanConstraint.ScanDataType switch
        {
            ScanDataType.Short => MeetsTheScanConstraint<short>(lhs, rhs, scanConstraint),
            ScanDataType.Integer => MeetsTheScanConstraint<int>(lhs, rhs, scanConstraint),
            ScanDataType.Float => MeetsTheScanConstraint<float>(lhs, rhs, scanConstraint),
            ScanDataType.Double => MeetsTheScanConstraint<double>(lhs, rhs, scanConstraint),
            ScanDataType.Long => MeetsTheScanConstraint<long>(lhs, rhs, scanConstraint),
            _ => throw new NotImplementedException($"Parsing string to Type: {scanConstraint.ScanDataType} not implemented")
        };
    }

    public static bool MeetsTheScanConstraint<T>(string lhs, string rhs, ScanConstraint scanConstraint)
        where T : INumber<T>
    {
        if (!lhs.TryParseNumber<T>(out var lhsValue) || !rhs.TryParseNumber<T>(out var rhsValue))
            return false;

        return MeetsTheScanConstraint(lhsValue, rhsValue, scanConstraint);
    }

    public static bool MeetsTheScanConstraint<T>(T lhs, T rhs, ScanConstraint scanConstraint) 
        where T : INumber<T>
    {
        return scanConstraint.ScanCompareType switch
        {
            ScanCompareType.ExactValue => lhs == rhs,
            ScanCompareType.SmallerThan => lhs < rhs,
            ScanCompareType.BiggerThan => lhs > rhs,
            _ => throw new NotImplementedException("Not implemented")
        };
    }

    public IList<IMemorySegment> GetMatchingMemorySegments(IList<VirtualMemoryRegion> virtualMemoryRegions,
                                                           IProgress<float>? progressBarUpdater = null,
                                                           CancellationToken token = default)
    {
        var matchingProcessMemories = new List<IMemorySegment>();

        foreach (var virtualMemoryRegion in virtualMemoryRegions)
        {
            if (token.IsCancellationRequested)
                break;

            var regionBytesAsSpan = virtualMemoryRegion.Bytes.AsSpan();

            for (var i = 0; i < (int)virtualMemoryRegion.RegionSize; i += _sizeOfT)
            {
                if (i + _sizeOfT > (int)virtualMemoryRegion.RegionSize)
                {
                    break;
                }
                var memoryValue = regionBytesAsSpan.Slice(i, _sizeOfT).ConvertToString(_scanConstraint.ScanDataType);

                if (MeetsTheScanConstraint(memoryValue, _userInput, _scanConstraint))
                {
                    matchingProcessMemories.Add(new MemorySegment(virtualMemoryRegion.BaseAddress, i, memoryValue, _scanConstraint.ScanDataType));
                }
            }
        }

        return matchingProcessMemories;
    }
}
