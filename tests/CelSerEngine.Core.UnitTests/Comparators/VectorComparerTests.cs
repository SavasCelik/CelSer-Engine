using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Comparators;

public class VectorComparerTests
{
    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_Short_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedComparToTest<short>(scanCompareType, ScanDataType.Short);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_Integer_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedComparToTest<int>(scanCompareType, ScanDataType.Integer);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_Long_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedComparToTest<long>(scanCompareType, ScanDataType.Long);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_Float_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedComparToTest<float>(scanCompareType, ScanDataType.Float);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_Double_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedComparToTest<double>(scanCompareType, ScanDataType.Double);
    }

    private void RunRandomizedComparToTest<T>(ScanCompareType compareType, ScanDataType scanDataType)
        where T : struct, INumber<T>
    {
        T[] values = Util.GenerateRandomValuesForVector<T>();
        int searchedValueIndex = Util.GenerateSingleValue<int>(0, values.Length - 1);
        var searchedValue = values[searchedValueIndex];
        var bytes = MemoryMarshal.AsBytes(values.AsSpan());
        var resultArray = new T[values.Length];
        var scanConstraint = new ScanConstraint(compareType, scanDataType, searchedValue.ToString() ?? "");
        var vectorComparer = new VectorComparer<T>(scanConstraint);

        var result = vectorComparer.CompareTo(bytes).As<byte, T>();
        result.CopyTo(resultArray);

        var expectedCount = GetExpectedCount(compareType, values, searchedValue);
        var actualCount = resultArray.Count(x => x != T.Zero);

        Assert.Equal(expectedCount, actualCount);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_Short_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedGetMatchingMemorySegmentsTest<short>(scanCompareType, ScanDataType.Short);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_Integer_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedGetMatchingMemorySegmentsTest<int>(scanCompareType, ScanDataType.Integer);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_Long_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedGetMatchingMemorySegmentsTest<long>(scanCompareType, ScanDataType.Long);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_Float_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedGetMatchingMemorySegmentsTest<float>(scanCompareType, ScanDataType.Float);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_Double_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedGetMatchingMemorySegmentsTest<double>(scanCompareType, ScanDataType.Double);
    }

    private void RunRandomizedGetMatchingMemorySegmentsTest<T>(ScanCompareType scanCompareType, ScanDataType scanDataType)
        where T : struct, INumber<T>
    {
        T[] values = Util.GenerateRandomValuesForVector<T>(Vector<T>.Count * 50);
        var searchedValueIndex = Util.GenerateSingleValue<int>(0, values.Length);
        var searchedValue = values[searchedValueIndex];
        var scanConstraint = new ScanConstraint(scanCompareType, scanDataType, searchedValue.ToString() ?? "");
        var byteArray = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        var virtualMemoryRegions = new List<VirtualMemoryRegion>()
        {
            new VirtualMemoryRegion(new IntPtr(0x1337), (ulong)byteArray.Length, byteArray)
        };
        var comparer = new VectorComparer<T>(scanConstraint);

        // Act
        IList<IMemorySegment> foundSegments = comparer.GetMatchingMemorySegments(virtualMemoryRegions, null);

        // Assert
        var expectedCount = GetExpectedCount(scanCompareType, values, searchedValue);
        Assert.Equal(expectedCount, foundSegments.Count);
    }

    public static IEnumerable<object[]> ScanCompareTypes()
    {
        yield return new object[] { ScanCompareType.ExactValue };
        yield return new object[] { ScanCompareType.SmallerThan };
        yield return new object[] { ScanCompareType.BiggerThan };
    }

    private int GetExpectedCount<T>(ScanCompareType compareType, T[] values, T searchedValue)
        where T : struct, INumber<T>
    {
        if (compareType == ScanCompareType.ExactValue)
            return values.Count(x => x == searchedValue);
        else if (compareType == ScanCompareType.SmallerThan)
            return values.Count(x => x < searchedValue);
        else if (compareType == ScanCompareType.BiggerThan)
            return values.Count(x => x > searchedValue);

        return 0;
    }
}
