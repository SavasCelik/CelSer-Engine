using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.UnitTests.SerializationTests;


public sealed class PointerBitLayoutTests
{
    [Fact]
    public void Ctor_ValidInputs_ComputesCorrectBitCounts()
    {
        // Arrange
        int maxModuleIndex = 15;   // 0b1111  -> 4 bits
        uint maxBaseOffset = 255;  // 0b11111111 -> 8 bits
        int maxLevel = 7;          // 0b111 -> 3 bits
        int maxOffset = 31;        // 0b11111 -> 5 bits

        // Act
        var layout = new PointerBitLayout(
            maxModuleIndex,
            maxBaseOffset,
            maxLevel,
            maxOffset
        );

        // Assert
        Assert.Equal(4, layout.MaxBitCountModuleIndex);
        Assert.Equal(8, layout.MaxBitCountModuleBaseOffset);
        Assert.Equal(3, layout.MaxBitCountLevel);
        Assert.Equal(5, layout.MaxBitCountOffset);
    }

    [Fact]
    public void Ctor_ValidInputs_ComputesCorrectEntrySizeInBytes()
    {
        // Arrange
        int maxModuleIndex = 7;   // 3 bits
        uint maxBaseOffset = 15;  // 4 bits
        int maxLevel = 3;         // 2 bits
        int maxOffset = 32;       // 6 bits

        /*
            Total bits:
            moduleIndex     = 3
            baseOffset      = 4
            sign            = 1
            level           = 2
            offsets         = 6 * maxLevel (3) = 18
            ----------------------------------
            total           = 28 bits -> rounds up to 4 bytes
        */

        // Act
        var layout = new PointerBitLayout(
            maxModuleIndex,
            maxBaseOffset,
            maxLevel,
            maxOffset
        );

        // Assert
        Assert.Equal(4, layout.EntrySizeInBytes);
    }

    [Fact]
    public void Ctor_BaseOffsetBitCountLessThan32_ComputesCorrectMask()
    {
        // Arrange
        uint maxBaseOffset = 1023; // 10 bits

        // Act
        var layout = new PointerBitLayout(
            maxModuleIndex: 1,
            maxBaseOffset,
            maxLevel: 1,
            maxOffset: 1
        );

        // Assert
        Assert.Equal((1u << 10) - 1, layout.MaskModuleBaseOffset);
    }

    [Fact]
    public void Ctor_BaseOffsetBitCountEquals32_UsesFullUintMask()
    {
        // Arrange
        uint maxBaseOffset = uint.MaxValue; // 32 bits

        // Act
        var layout = new PointerBitLayout(
            maxModuleIndex: 1,
            maxBaseOffset,
            maxLevel: 1,
            maxOffset: 1
        );

        // Assert
        Assert.Equal(uint.MaxValue, layout.MaskModuleBaseOffset);
    }

    [Fact]
    public void Ctor_ValidInputs_ComputesCorrectOtherMasks()
    {
        // Arrange
        int maxModuleIndex = 31; // 5 bits
        int maxLevel = 15;       // 4 bits
        int maxOffset = 7;       // 3 bits

        // Act
        var layout = new PointerBitLayout(
            maxModuleIndex,
            maxModuleOffset: 1,
            maxLevel,
            maxOffset
        );

        // Assert
        Assert.Equal((1 << 5) - 1, layout.MaskModuleIndex);
        Assert.Equal((1 << 4) - 1, layout.MaskLevel);
        Assert.Equal((1 << 3) - 1, layout.MaskOffset);
    }

    [Fact]
    public void SignConstants_AreCorrect()
    {
        // Assert
        Assert.Equal(1, PointerBitLayout.SignBitCount);
        Assert.Equal(0b1, PointerBitLayout.SignMask);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-42)]
    public void Ctor_InvalidMaxModuleIndex_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PointerBitLayout(
                maxModuleIndex: invalidValue,
                maxModuleOffset: 1,
                maxLevel: 1,
                maxOffset: 1
            )
        );

        // Assert
        Assert.Equal("maxModuleIndex", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(int.MaxValue)]
    public void Ctor_InvalidMaxLevel_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PointerBitLayout(
                maxModuleIndex: 1,
                maxModuleOffset: 1,
                maxLevel: invalidValue,
                maxOffset: 1
            )
        );

        // Assert
        Assert.Equal("maxLevel", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Ctor_InvalidMaxOffset_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PointerBitLayout(
                maxModuleIndex: 1,
                maxModuleOffset: 1,
                maxLevel: 1,
                maxOffset: invalidValue
            )
        );

        // Assert
        Assert.Equal("maxOffset", exception.ParamName);
    }

    [Fact]
    public void Ctor_MaxLevelEquals30_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() =>
            new PointerBitLayout(
                maxModuleIndex: 1,
                maxModuleOffset: 1,
                maxLevel: IPointerLayout.MaxSupportedLevel,
                maxOffset: 1
            )
        );

        // Assert
        Assert.Null(exception);
    }
}
