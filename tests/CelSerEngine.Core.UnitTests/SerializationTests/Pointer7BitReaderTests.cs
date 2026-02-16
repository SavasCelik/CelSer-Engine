using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.UnitTests.SerializationTests;

public sealed class Pointer7BitReaderTests
{
    [Fact]
    public void ReadFromBuffer_SmallValues_ReadsCorrectly()
    {
        // Arrange
        var layout = new Pointer7BitLayout(
            maxModuleIndex: 127,
            maxModuleOffset: 1024,
            maxLevel: 3,
            maxOffset: 127
        );
        var buffer = new byte[layout.EntrySizeInBytes];
        var values = new byte[] { 0x05, 0x02, 0x01, 0x0A }; // baseOffset=5,moduleIndex=2,level=1,offset=10
        values.CopyTo(buffer, 0);
        using var reader = new Pointer7BitReader(new MemoryStream(buffer), layout, CreateModules());

        // Act
        var pointer = reader.Read();

        // Assert
        Assert.Equal("Game", pointer.ModuleName);
        Assert.Equal(0x30000000, pointer.BaseAddress);
        Assert.Equal(5, pointer.BaseOffset);
        Assert.Single(pointer.Offsets);
        Assert.Equal(10, pointer.Offsets[0]);
    }

    [Fact]
    public void ReadFromBuffer_LargeValues_ReadsCorrectly()
    {
        // Arrange
        var layout = new Pointer7BitLayout(
            maxModuleIndex: 127,
            maxModuleOffset: 1024,
            maxLevel: 2,
            maxOffset: 127
        );
        var buffer = new byte[layout.EntrySizeInBytes];
        // baseOffset=1000 -> 0xE8 0x07, moduleIndex=0 -> 0x00 , level+1=2 -> 0x02, offset[0]=200 -> 0xC8 0x01, offset[0]=100 -> 0x64
        var values = new byte[] { 0xE8, 0x07, 0x00, 0x02, 0xC8, 0x01, 0x64 };
        values.CopyTo(buffer, 0);
        using var reader = new Pointer7BitReader(new MemoryStream(buffer), layout, CreateModules());

        // Act
        var pointer = reader.Read();

        // Assert
        Assert.Equal("Kernel32", pointer.ModuleName);
        Assert.Equal(1000, pointer.BaseOffset);
        Assert.Equal(2, pointer.Offsets.Count);
        Assert.Equal(200, pointer.Offsets[0]);
        Assert.Equal(0x64, pointer.Offsets[1]);
    }

    private static IReadOnlyList<ModuleInfo> CreateModules() =>
        [
            new ModuleInfo { Name = "Kernel32", BaseAddress = 0x10000000 },
            new ModuleInfo { Name = "User32",   BaseAddress = 0x20000000 },
            new ModuleInfo { Name = "Game",     BaseAddress = 0x30000000 }
        ];
}
