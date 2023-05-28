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

    public static bool CompareDataByScanConstraintType(string lhs, string rhs, ScanConstraint scanConstraint)
    {
        return scanConstraint.ScanDataType switch
        {
            ScanDataType.Short => CompareDataByScanConstraintType<short>(lhs, rhs, scanConstraint),
            ScanDataType.Integer => CompareDataByScanConstraintType<int>(lhs, rhs, scanConstraint),
            ScanDataType.Float => CompareDataByScanConstraintType<float>(lhs, rhs, scanConstraint),
            ScanDataType.Double => CompareDataByScanConstraintType<double>(lhs, rhs, scanConstraint),
            ScanDataType.Long => CompareDataByScanConstraintType<long>(lhs, rhs, scanConstraint),
            _ => throw new NotImplementedException($"Parsing string to Type: {scanConstraint.ScanDataType} not implemented")
        };
    }

    public static bool CompareDataByScanConstraintType<T>(string lhs, string rhs, ScanConstraint scanConstraint)
        where T : INumber<T>
    {
        T lhsValue = lhs.ParseToINumberT<T>();
        T rhsValue = rhs.ParseToINumberT<T>();

        return CompareDataByScanConstraintType(lhsValue, rhsValue, scanConstraint);
    }

    public static bool CompareDataByScanConstraintType<T>(T lhs, T rhs, ScanConstraint scanConstraint) 
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

                if (CompareDataByScanConstraintType(valueObject, _userInput, _scanConstraint))
                {
                    yield return new ProcessMemory(virtualMemoryPage.BaseAddress, i, bufferValue.ByteArrayToObject(_scanConstraint.ScanDataType), _scanConstraint.ScanDataType);
                }
            }
        }
    }
}
