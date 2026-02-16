using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.UnitTests.SerializationTests;


public sealed class Pointer7BitLayoutTests
{
    [Fact]
    public void Ctor_SmallValues_ComputesCorrectEntrySize()
    {
        // Arrange
        int maxModuleIndex = 127;    // 1 byte
        uint maxModuleOffset = 1024; // fixed 10 bytes
        int maxLevel = 3;            // 1 byte
        int maxOffset = 15;          // 1 byte

        // Act
        var layout = new Pointer7BitLayout(maxModuleIndex, maxModuleOffset, maxLevel, maxOffset);

        // Assert
        Assert.Equal(15, layout.EntrySizeInBytes);
    }

    [Fact]
    public void Ctor_LargeValues_ComputesEntrySizeCorrectly()
    {
        // Arrange
        int maxModuleIndex = 16384;   // 3 bytes
        uint maxModuleOffset = 1024;  // fixed 10 bytes
        int maxLevel = 25;            // 1 bytes
        int maxOffset = 1024;         // 2 bytes

        var layout = new Pointer7BitLayout(maxModuleIndex, maxModuleOffset, maxLevel, maxOffset);

        // Assert
        Assert.Equal(64, layout.EntrySizeInBytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-42)]
    public void Ctor_InvalidMaxModuleIndex_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Pointer7BitLayout(
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
            new Pointer7BitLayout(
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
            new Pointer7BitLayout(
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
            new Pointer7BitLayout(
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
