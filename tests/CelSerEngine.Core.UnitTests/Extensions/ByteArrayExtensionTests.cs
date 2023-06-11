using CelSerEngine.Core.Models;
using CelSerEngine.Core.Extensions;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Extensions;

public class ByteArrayExtensionTests
{
    [Fact]
    public void ConvertToString_ShortByteArray_ReturnsShortString()
    {
        // Arrange
        byte[] byteArray = BitConverter.GetBytes((short)42);
        ScanDataType scanDataType = ScanDataType.Short;
        string expected = BitConverter.ToInt16(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_IntegerByteArray_ReturnsIntegerString()
    {
        // Arrange
        byte[] byteArray = BitConverter.GetBytes(42);
        ScanDataType scanDataType = ScanDataType.Integer;
        string expected = BitConverter.ToInt32(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_FloatByteArray_ReturnsFloatString()
    {
        // Arrange
        byte[] byteArray = BitConverter.GetBytes(3.14f);
        ScanDataType scanDataType = ScanDataType.Float;
        string expected = BitConverter.ToSingle(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_DoubleByteArray_ReturnsDoubleString()
    {
        // Arrange
        byte[] byteArray = BitConverter.GetBytes(3.14159);
        ScanDataType scanDataType = ScanDataType.Double;
        string expected = BitConverter.ToDouble(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_LongByteArray_ReturnsLongString()
    {
        // Arrange
        byte[] byteArray = BitConverter.GetBytes(42L);
        ScanDataType scanDataType = ScanDataType.Long;
        string expected = BitConverter.ToInt64(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_UnsupportedType_ByteArray_ThrowsException()
    {
        // Arrange
        byte[] byteArray = new byte[4];
        ScanDataType scanDataType = (ScanDataType)99;

        // Act and Assert
        Assert.Throws<NotSupportedException>(() => byteArray.ConvertToString(scanDataType));
    }

    [Fact]
    public void ConvertToString_ShortByteSpan_ReturnsShortString()
    {
        // Arrange
        Span<byte> byteArray = BitConverter.GetBytes((short)42);
        ScanDataType scanDataType = ScanDataType.Short;
        string expected = BitConverter.ToInt16(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_IntegerByteSpan_ReturnsIntegerString()
    {
        // Arrange
        Span<byte> byteArray = BitConverter.GetBytes(42);
        ScanDataType scanDataType = ScanDataType.Integer;
        string expected = BitConverter.ToInt32(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_FloatByteSpan_ReturnsFloatString()
    {
        // Arrange
        Span<byte> byteArray = BitConverter.GetBytes(3.14f);
        ScanDataType scanDataType = ScanDataType.Float;
        string expected = BitConverter.ToSingle(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_DoubleByteSpan_ReturnsDoubleString()
    {
        // Arrange
        Span<byte> byteArray = BitConverter.GetBytes(3.14159);
        ScanDataType scanDataType = ScanDataType.Double;
        string expected = BitConverter.ToDouble(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_LongByteSpan_ReturnsLongString()
    {
        // Arrange
        Span<byte> byteArray = BitConverter.GetBytes(42L);
        ScanDataType scanDataType = ScanDataType.Long;
        string expected = BitConverter.ToInt64(byteArray).ToString();

        // Act
        string result = byteArray.ConvertToString(scanDataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToString_UnsupportedType_ByteSpan_ThrowsException()
    {
        // Arrange
        byte[] byteArray = new byte[4];
        ScanDataType scanDataType = (ScanDataType)99;

        // Act and Assert
        Assert.Throws<NotSupportedException>(() =>
        {
            Span<byte> spanByteArray = new Span<byte>(byteArray);
            spanByteArray.ConvertToString(scanDataType);
        });
    }
}
