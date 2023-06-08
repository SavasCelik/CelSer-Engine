using CelSerEngine.Core.Models;
using CelSerEngine.Core.Extensions;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Extensions;

public class ScanDataTypeExtensionTests
{
    [Theory]
    [InlineData(ScanDataType.Short, sizeof(short))]
    [InlineData(ScanDataType.Integer, sizeof(int))]
    [InlineData(ScanDataType.Float, sizeof(float))]
    [InlineData(ScanDataType.Double, sizeof(double))]
    [InlineData(ScanDataType.Long, sizeof(long))]
    public void GetPrimitiveSize_ReturnsCorrectSize(ScanDataType scanDataType, int expectedSize)
    {
        int result = scanDataType.GetPrimitiveSize();
        Assert.Equal(expectedSize, result);
    }

    [Theory]
    [InlineData((ScanDataType)100)]
    [InlineData((ScanDataType)200)]
    [InlineData((ScanDataType)300)]
    public void GetPrimitiveSize_NotSupported_ThrowsException(ScanDataType scanDataType)
    {
        Assert.Throws<NotSupportedException>(() => scanDataType.GetPrimitiveSize());
    }
}
