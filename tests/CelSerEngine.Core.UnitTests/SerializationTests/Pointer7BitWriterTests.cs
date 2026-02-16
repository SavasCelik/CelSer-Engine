using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.UnitTests.SerializationTests;

public sealed class Pointer7BitWriterTests
{
    [Fact]
    public void Write_ValidInput_WritesExpectedNumberOfBytes()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = new Pointer7BitLayout(
            maxModuleIndex: 10,
            maxModuleOffset: 452,
            maxLevel: 20,
            maxOffset: 0x100
        );
        using var writer = new Pointer7BitWriter(stream, layout);

        // Act
        writer.Write(
            level: 1,
            moduleIndex: 3,
            baseOffset: 10,
            offsets: [0x10, 0x20]
        );

        // Assert
        Assert.Equal(layout.EntrySizeInBytes, stream.Length);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(-420)]
    [InlineData(777)]
    [InlineData(-123456)]
    public void Write_BaseOffset_WritesBytesCorrectly(long baseOffset)
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = new Pointer7BitLayout(
            maxModuleIndex: 4,
            maxModuleOffset: uint.MaxValue,
            maxLevel: 4,
            maxOffset: 4
        );
        using var writer = new Pointer7BitWriter(stream, layout);

        // Act
        writer.Write(
            level: 2,
            moduleIndex: 4,
            baseOffset: baseOffset,
            offsets: [0x10, 0x20, 0x30]
        );

        stream.Position = 0;
        using var br = new BinaryReader(stream);

        // Assert
        Assert.Equal(baseOffset, br.Read7BitEncodedInt64());
    }

    [Fact]
    public void Write_SmallValues_CreatesExpectedBytes()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = new Pointer7BitLayout(
            maxModuleIndex: 4,
            maxModuleOffset: uint.MaxValue,
            maxLevel: 4,
            maxOffset: 4
        );
        using var writer = new Pointer7BitWriter(stream, layout);

        // Act
        writer.Write(
            level: 0, 
            moduleIndex: 2, 
            baseOffset: 5, 
            offsets: [10]
        );

        var bytes = stream.ToArray();

        // Assert
        // Manually verify the 7-bit encoding for small values
        // baseOffset=5 -> 0x05
        // moduleIndex=2 -> 0x02
        // level+1=1 -> 0x01
        // offset=10 -> 0x0A
        Assert.Equal(new byte[] { 0x05, 0x02, 0x01, 0x0A }, bytes[..4]);
    }

    [Fact]
    public void Write_LargeValues_UsesMultipleBytes()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = new Pointer7BitLayout(
            maxModuleIndex: 300,
            maxModuleOffset: uint.MaxValue,
            maxLevel: 4,
            maxOffset: 200
        );
        using var writer = new Pointer7BitWriter(stream, layout);

        // Act
        writer.Write(
            level: 0,
            moduleIndex: 300, // >127 requires 2 bytes
            baseOffset: 1000, // >127 requires 2 bytes
            offsets: [200]    // >127 requires 2 bytes
        );

        var bytes = stream.ToArray();

        // Assert
        // baseOffset=1000 -> 0xE8 0x07
        // moduleIndex=300 -> 0xAC 0x02
        // level+1=1 -> 0x01
        // offset=200 -> 0xC8 0x01
        Assert.Equal(new byte[] { 0xE8, 0x07, 0xAC, 0x02, 0x01, 0xC8, 0x01 }, bytes[..7]);
    }
}
