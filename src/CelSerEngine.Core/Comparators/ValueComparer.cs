using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;
using System.Numerics;

namespace CelSerEngine.Core.Comparators;

public class ValueComparer : IScanComparer
{
    private readonly ScanConstraint _scanConstraint;
    private string _userInput;
    private string _userInputToValue;
    private readonly int _sizeOfT;

    public ValueComparer(ScanConstraint scanConstraint)
    {
        _scanConstraint = scanConstraint;
        
        if (scanConstraint.ScanCompareType == ScanCompareType.ValueBetween)
        {
            var hyphenIndex = scanConstraint.UserInput.IndexOf('-');
            if (hyphenIndex == -1)
            {
                throw new ArgumentException("Invalid input for ValueBetween scan type");
            }

            var userInputFrom = scanConstraint.UserInput[..hyphenIndex];
            var userInputTo = scanConstraint.UserInput[(hyphenIndex + 1)..];
            _userInput = userInputFrom;
            _userInputToValue = userInputTo;
        }
        else
        {
            _userInput = scanConstraint.UserInput;
        }

        _sizeOfT = scanConstraint.ScanDataType.GetPrimitiveSize();
    }

    public bool MeetsTheScanConstraint(string value)
    {
        return _scanConstraint.ScanDataType switch
        {
            ScanDataType.Short => MeetsTheScanConstraint<short>(value),
            ScanDataType.Integer => MeetsTheScanConstraint<int>(value),
            ScanDataType.Float => MeetsTheScanConstraint<float>(value),
            ScanDataType.Double => MeetsTheScanConstraint<double>(value),
            ScanDataType.Long => MeetsTheScanConstraint<long>(value),
            _ => throw new NotImplementedException($"Parsing string to Type: {_scanConstraint.ScanDataType} not implemented")
        };
    }

    public bool MeetsTheScanConstraint<T>(string value)
        where T : INumber<T>
    {
        if (!value.TryParseNumber<T>(out var valueParsed))
            return false;

        return MeetsTheScanConstraint(valueParsed);
    }

    public bool MeetsTheScanConstraint<T>(T valueParsed) 
        where T : INumber<T>
    {
        var userInputParsed = _userInput.ParseNumber<T>();

        return _scanConstraint.ScanCompareType switch
        {
            ScanCompareType.ExactValue => valueParsed == userInputParsed,
            ScanCompareType.SmallerThan => valueParsed < userInputParsed,
            ScanCompareType.BiggerThan => valueParsed > userInputParsed,
            ScanCompareType.ValueBetween => _userInputToValue.TryParseNumber<T>(out var userInputToValueParsed) && valueParsed >= userInputParsed && valueParsed <= userInputToValueParsed,
            ScanCompareType.UnknownInitialValue => true,
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

                if (MeetsTheScanConstraint(memoryValue))
                {
                    matchingProcessMemories.Add(new MemorySegment(virtualMemoryRegion.BaseAddress, i, memoryValue, _scanConstraint.ScanDataType));
                }
            }
        }

        return matchingProcessMemories;
    }
}
