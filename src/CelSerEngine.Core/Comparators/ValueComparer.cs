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
        T lhsValue = lhs.ParseToStruct<T>();
        T rhsValue = rhs.ParseToStruct<T>();

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

    public IEnumerable<ProcessMemory> GetMatchingValueAddresses(IList<VirtualMemoryPage> virtualMemoryPages, IProgress<float> progressBarUpdater)
    {
        foreach (var virtualMemoryPage in virtualMemoryPages)
        {
            for (var i = 0; i < (int)virtualMemoryPage.RegionSize; i += _sizeOfT)
            {
                if (i + _sizeOfT > (int)virtualMemoryPage.RegionSize)
                {
                    break;
                }
                var bufferValue = virtualMemoryPage.Bytes.AsSpan().Slice(i, _sizeOfT).ToArray();
                var valueObject = bufferValue.ByteArrayToObject(_scanConstraint.ScanDataType);

                if (MeetsTheScanConstraint(valueObject, _userInput, _scanConstraint))
                {
                    yield return new ProcessMemory(virtualMemoryPage.BaseAddress, i, valueObject, _scanConstraint.ScanDataType);
                }
            }
        }
    }
}
