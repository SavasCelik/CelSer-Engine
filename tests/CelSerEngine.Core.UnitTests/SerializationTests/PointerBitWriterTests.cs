using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.UnitTests.SerializationTests;

public sealed class PointerBitWriterTests
{
    [Fact]
    public void Write_ValidInput_WritesExpectedNumberOfBytes()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = new PointerBitLayout(
            maxModuleIndex: 10,
            maxModuleOffset: 452,
            maxLevel: 20,
            maxOffset: 0x100
        );
        using var writer = new PointerBitWriter(stream, layout);

        // Act
        writer.Write(
            level: 1,
            moduleIndex: 3,
            baseOffset: 10,
            offsets: [ 0x10, 0x20 ]
        );

        // Assert
        Assert.Equal(layout.EntrySizeInBytes, stream.Length);
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(-420, true)]
    [InlineData(777, false)]
    [InlineData(-123456, true)]
    public void Write_BaseOffset_SetsSignBitCorrectly(long baseOffset, bool expectedSignBit)
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = new PointerBitLayout(
            maxModuleIndex: 4,
            maxModuleOffset: 1 << 15, // 16 bits
            maxLevel: 4,
            maxOffset: 4
        );
        using var writer = new PointerBitWriter(stream, layout);

        // Act
        writer.Write(
            level: 2,
            moduleIndex: 4,
            baseOffset: baseOffset,
            offsets: [0x10, 0x20, 0x30]
        );

        var bytes = stream.ToArray();

        // Assert
        // baseOffset occupies the first 16 bits (index 0 and 1) so the next bit written is the sign bit
        bool signBitSet = (bytes[2] & PointerBitLayout.SignMask) != 0;
        Assert.Equal(expectedSignBit, signBitSet);
    }

    [Fact]
    public void Write_BitPackingCrossesByteBoundary_PacksBitsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();

        var layout = new PointerBitLayout(
            maxModuleOffset: 0b1_1111, // 0b1_1111
            maxModuleIndex: 0b11_1111, // forces boundary crossing
            maxLevel: 0b1111,
            maxOffset: 452
        );

        using var writer = new PointerBitWriter(stream, layout);

        /*
            Write order:
            baseOffset = 0b10101 (5 bits)
            sign       = 0b0     (1 bit)
            moduleIdx  = 0b11_0011 (6 bits) <- crosses boundary
            level+1    = 0b0010  (4 bits)

            Bit stream:

            Byte 0: 11 0 10101  -> 0b10101 (baseOffset), 0b0 (signBit), 0b11 (first 2bits of moduleIdx)
            Byte 1: 0010 1100   -> 0b0011 (rest of moduleIdx), 0b0010 (level)
        */

        // Act
        writer.Write(
            level: 1,
            moduleIndex: 0b110011,
            baseOffset: 0b10101,
            offsets: []
        );

        var bytes = stream.ToArray();

        // Assert
        Assert.Equal(2, bytes.Where(x => x != 0).Count());
        Assert.Equal(0b11_0_10101, bytes[0]);
        Assert.Equal(0b0010_1100, bytes[1]);
    }
}
