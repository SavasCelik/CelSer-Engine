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
    public void CompareTo_ShortRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_CompareTo<short>(scanCompareType, ScanDataType.Short);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_IntegerRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_CompareTo<int>(scanCompareType, ScanDataType.Integer);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_LongRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_CompareTo<long>(scanCompareType, ScanDataType.Long);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_FloatRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_CompareTo<float>(scanCompareType, ScanDataType.Float);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void CompareTo_DoubleRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_CompareTo<double>(scanCompareType, ScanDataType.Double);
    }

    private void RunRandomizedTest_CompareTo<T>(ScanCompareType compareType, ScanDataType scanDataType)
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
    public void GetMatchingMemorySegments_ShortRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_GetMatchingMemorySegments<short>(scanCompareType, ScanDataType.Short);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_IntegerRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_GetMatchingMemorySegments<int>(scanCompareType, ScanDataType.Integer);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_LongRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_GetMatchingMemorySegments<long>(scanCompareType, ScanDataType.Long);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_FloatRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_GetMatchingMemorySegments<float>(scanCompareType, ScanDataType.Float);
    }

    [Theory]
    [MemberData(nameof(ScanCompareTypes))]
    public void GetMatchingMemorySegments_DoubleRandom_ReturnsCorrectCount(ScanCompareType scanCompareType)
    {
        RunRandomizedTest_GetMatchingMemorySegments<double>(scanCompareType, ScanDataType.Double);
    }

    private void RunRandomizedTest_GetMatchingMemorySegments<T>(ScanCompareType scanCompareType, ScanDataType scanDataType)
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

    [Theory]
    [MemberData(nameof(Short_Scan_TestData))]
    public void GetMatchingMemorySegments_Short_ReturnsCorrectCount(ScanCompareType scanCompareType, short[] values, string searchedValue, int expectedCount)
    {
        var scanConstraint = new ScanConstraint(scanCompareType, ScanDataType.Short, searchedValue);
        var valuesByteArray = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        var virtualMemoryRegions = new List<VirtualMemoryRegion>()
        {
            new VirtualMemoryRegion(new IntPtr(0x1337), (ulong)valuesByteArray.Length, valuesByteArray)
        };
        var comparer = new VectorComparer<short>(scanConstraint);

        // Act
        IList<IMemorySegment> foundSegments = comparer.GetMatchingMemorySegments(virtualMemoryRegions, null);

        // Assert
        Assert.Equal(expectedCount, foundSegments.Count);
    }

    public static IEnumerable<object[]> Short_Scan_TestData()
    {
        var values = new short[32] { 7, 452, 2, 7, 11, 19, 5, 2, 9, 26, 16, 452, 8, 17, 4, 7, 20, 10, 30, 13, 1, 25, 6, 12, 3, 24, 15, 30, 22, 21, 18, 27 };

        // ExactValue
        yield return new object[] { ScanCompareType.ExactValue, values, "452", 2 };
        yield return new object[] { ScanCompareType.ExactValue, values, "7", 3 };
        yield return new object[] { ScanCompareType.ExactValue, values, "2", 2 };
        yield return new object[] { ScanCompareType.ExactValue, values, "11", 1 };
        yield return new object[] { ScanCompareType.ExactValue, values, "679", 0 };

        // SmallerThan
        yield return new object[] { ScanCompareType.SmallerThan, values, "452", 30 };
        yield return new object[] { ScanCompareType.SmallerThan, values, "7", 7 };
        yield return new object[] { ScanCompareType.SmallerThan, values, "2", 1 };
        yield return new object[] { ScanCompareType.SmallerThan, values, "679", 32 };

        // BiggerThan
        yield return new object[] { ScanCompareType.BiggerThan, values, "452", 0 };
        yield return new object[] { ScanCompareType.BiggerThan, values, "7", 22 };
        yield return new object[] { ScanCompareType.BiggerThan, values, "2", 29 };
        yield return new object[] { ScanCompareType.BiggerThan, values, "679", 0 };
    }
}   

