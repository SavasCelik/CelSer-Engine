using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public class ValueComparer : IScanComparer
{
    private readonly ScanConstraint _scanConstraint;
    private dynamic _userInput;
    private readonly int _sizeOfT;

    public ValueComparer(ScanConstraint scanConstraint)
    {
        _scanConstraint = scanConstraint;
        _userInput = scanConstraint.UserInput;
        _sizeOfT = scanConstraint.ScanDataType.GetPrimitiveSize();
    }

    public static bool CompareDataByScanConstraintType(dynamic lhs, dynamic rhs, ScanCompareType scanConstraintType)
    {
        if (!((Type)lhs.GetType()).IsValueType)
            throw new ArgumentException("lhs must be a ValueType (struct)");

        if (!((Type)rhs.GetType()).IsValueType)
            throw new ArgumentException("rhs must be a ValueType (struct)");

        return scanConstraintType switch
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

                if (CompareDataByScanConstraintType(valueObject, _userInput, _scanConstraint.ScanCompareType))
                {
                    yield return new ProcessMemory(virtualMemoryPage.BaseAddress, i, bufferValue.ByteArrayToObject(_scanConstraint.ScanDataType), _scanConstraint.ScanDataType);
                }
            }
        }
    }
}
