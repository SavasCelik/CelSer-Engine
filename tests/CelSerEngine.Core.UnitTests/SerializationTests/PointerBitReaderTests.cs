using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.UnitTests.SerializationTests;

public sealed class PointerBitReaderTests
{
    [Fact]
    public void ReadFromBuffer_ValidInputs_ReadsCorrectly()
    {
        // Layout:
        // baseOffset: 4 bits
        // sign:       1 bit
        // moduleIdx:  2 bits
        // level:      2 bits
        // offset:     4 bits

        var layout = new PointerBitLayout(
            maxModuleIndex: 3,
            maxModuleOffset: 15,
            maxLevel: 2,
            maxOffset: 15
        );

        /*
            Bit stream:
            baseOffset = 5  (0101)
            sign       = 0
            moduleIdx  = 1  (01)
            level      = 1  (01)
            offset     = 7  (0111)

            Binary: 0111 01 01 0 0101 -> 0xEA5
        */

        using var stream = StreamFromBytes(BitConverter.GetBytes(0xEA5));
        var reader = new PointerBitReader(stream, layout, CreateModules());

        // Act
        var pointer = reader.Read();

        // Assert
        Assert.Equal("B", pointer.ModuleName);
        Assert.Equal(0x2000, pointer.BaseAddress);
        Assert.Equal(5, pointer.BaseOffset);
        Assert.Single(pointer.Offsets);
        Assert.Equal(7, pointer.Offsets[0]);
    }

    [Fact]
    public void ReadFromBuffer_NegativeBaseOffset_ReadsCorrectly()
    {
        var layout = new PointerBitLayout(
            maxModuleIndex: 1,
            maxModuleOffset: 15,
            maxLevel: 1,
            maxOffset: 1
        );

        /*
            baseOffset = 6  (0110)
            sign       = 1
            moduleIdx  = 0
            level      = 1
            offset     = 1

            Binary: 1 1 0 1 0110 -> 0xD6
        */

        using var stream = StreamFromBytes(0xD6);
        var reader = new PointerBitReader(stream, layout, CreateModules());

        // Act
        var pointer = reader.Read();

        // Assert
        Assert.Equal(-6, pointer.BaseOffset);
    }

    [Fact]
    public void ReadFromBuffer_OffsetsCrossByteBoundary_ReadsCorrectly()
    {
        var layout = new PointerBitLayout(
            maxModuleIndex: 1,
            maxModuleOffset: 3,
            maxLevel: 2,
            maxOffset: 7
        );

        /*
            baseOffset = 3 (11)
            sign       = 0
            moduleIdx  = 1 (1) 
            level      = 2 (10)
            offsets    = [5, 6] (101, 110)

            Binary: 110 101 10 1 0 11
            Bytes: 0110_1011, 0000_1101
        */

        using var stream = StreamFromBytes(0b0110_1011, 0b0000_1101);
        var reader = new PointerBitReader(stream, layout, CreateModules());

        // Act
        var pointer = reader.Read();

        // Assert
        Assert.Equal(2, pointer.Offsets.Count);
        Assert.Equal(5, pointer.Offsets[0]);
        Assert.Equal(6, pointer.Offsets[1]);
    }

    [Fact]
    public void ReadFromBuffer_ModuleIndex_ResolvesCorrectModule()
    {
        var layout = new PointerBitLayout(
            maxModuleIndex: 3,
            maxModuleOffset: 1,
            maxLevel: 1,
            maxOffset: 1
        );

        /*
            baseOffset = 0 (0)
            sign       = 0
            moduleIdx  = 2 (10) 
            level      = 0 (0)
            offsets    = 0

            Binary: 0 0 10 0 0 -> 0b0000_1000
        */

        using var stream = StreamFromBytes(0b0000_1000);
        var reader = new PointerBitReader(stream, layout, CreateModules());

        // Act
        var pointer = reader.Read();

        // Assert
        Assert.Equal("C", pointer.ModuleName);
        Assert.Equal(0x3000, pointer.BaseAddress);
    }


    private static MemoryStream StreamFromBytes(params byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return stream;
    }

    private static IReadOnlyList<ModuleInfo> CreateModules() =>
        [
        new ModuleInfo { Name = "A", BaseAddress = 0x1000 },
        new ModuleInfo { Name = "B", BaseAddress = 0x2000 },
        new ModuleInfo { Name = "C", BaseAddress = 0x3000 }
        ];
}
