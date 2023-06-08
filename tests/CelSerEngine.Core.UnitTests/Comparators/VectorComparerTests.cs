using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using System.Runtime.InteropServices;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Comparators;

public class VectorComparerTests
{
    [Fact]
    public void GetMatchingMemorySegments_Integer_ExactValue_ReturnsCorrectCount()
    {
        // Arrange
        var searchedValueInt = 452;
        var searchedValueString = "452";
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, searchedValueString);
        var intArray = new int[32]
        { 
            2435745, 345834, searchedValueInt, 123, 54654, searchedValueInt, 2394234, 32492395, 786, 718345,
            9609456, 349539, 3405345, searchedValueInt, 9353495, 35687, 48, 2132, 12365, 4546,
            3634, 567567, 7844, 2134111, 357305, 34590341, searchedValueInt, 873544, 345345, 12356,
            7007, 77784,
        };
        var byteArray = MemoryMarshal.AsBytes(intArray.AsSpan()).ToArray(); // Convert int array to byte array
        var virtualMemoryRegions = new List<VirtualMemoryRegion>()
        {
            new VirtualMemoryRegion(new IntPtr(0x1337), (ulong)byteArray.Length, byteArray)
        };
        var comparer = new VectorComparer<int>(scanConstraint);

        // Act
        IList<IMemorySegment> foundSegments = comparer.GetMatchingMemorySegments(virtualMemoryRegions, null);

        // Assert
        Assert.Equal(4, foundSegments.Count);
    }
}
