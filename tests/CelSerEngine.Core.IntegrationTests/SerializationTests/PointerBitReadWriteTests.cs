using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.SerializationTests;

public sealed class PointerBitReadWriteTests
{
    [Fact]
    public void WriteThenRead_RoundTrip_ReturnsOriginalValues()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = CreateLayout();
        var modules = CreateModules();

        var writer = new PointerBitWriter(stream, layout);
        var reader = new PointerBitReader(stream, layout, modules);

        var offsets = new IntPtr[] { 4, 8, 15 };

        // Act
        writer.Write(
            level: offsets.Length - 1,
            moduleIndex: 1,
            baseOffset: 128,
            offsets: offsets
        );

        stream.Position = 0;

        var pointer = reader.Read();

        // Assert
        Assert.Equal("User32", pointer.ModuleName);
        Assert.Equal(0x20000000, pointer.BaseAddress);
        Assert.Equal(128, pointer.BaseOffset);
        Assert.Equal(offsets, pointer.Offsets);
    }

    [Fact]
    public void WriteThenRead_NegativeBaseOffset_RoundTripsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = CreateLayout();
        var modules = CreateModules();

        var writer = new PointerBitWriter(stream, layout);
        var reader = new PointerBitReader(stream, layout, modules);

        // Act
        writer.Write(
            level: 0,
            moduleIndex: 0,
            baseOffset: -452,
            offsets: [42]
        );

        stream.Position = 0;

        var pointer = reader.Read();

        // Assert
        Assert.Equal("Kernel32", pointer.ModuleName);
        Assert.Equal(-452, pointer.BaseOffset);
        Assert.Single(pointer.Offsets);
        Assert.Equal(42, pointer.Offsets[0]);
    }

    [Fact]
    public void WriteThenRead_BitPackingCrossesByteBoundaries_RoundTripsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();

        var layout = new PointerBitLayout(
            maxModuleIndex: 3,
            maxModuleOffset: 1337,
            maxLevel: 3,
            maxOffset: 31
        );

        var modules = CreateModules();

        var writer = new PointerBitWriter(stream, layout);
        var reader = new PointerBitReader(stream, layout, modules);

        var offsets = new IntPtr[] { 7, 15, 31 };

        // Act
        writer.Write(
            level: offsets.Length - 1,
            moduleIndex: 2,
            baseOffset: -1337,
            offsets: offsets
        );

        stream.Position = 0;

        var pointer = reader.Read();

        // Assert
        Assert.Equal("Game", pointer.ModuleName);
        Assert.Equal(-1337, pointer.BaseOffset);
        Assert.Equal(offsets, pointer.Offsets);
    }

    [Fact]
    public void WriteThenRead_MaxSupportedLevel_RoundTripsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();

        const int maxLevel = IPointerLayout.MaxSupportedLevel;

        var layout = new PointerBitLayout(
            maxModuleIndex: 1,
            maxModuleOffset: 1024,
            maxLevel: maxLevel,
            maxOffset: maxLevel
        );

        var modules = CreateModules();

        var offsets = Enumerable.Range(0, maxLevel)
                                .Select(i => (IntPtr)i)
                                .ToArray();

        var writer = new PointerBitWriter(stream, layout);
        var reader = new PointerBitReader(stream, layout, modules);

        // Act
        writer.Write(
            level: maxLevel - 1,
            moduleIndex: 0,
            baseOffset: 0,
            offsets: offsets
        );

        stream.Position = 0;

        var pointer = reader.Read();

        // Assert
        Assert.Equal(offsets, pointer.Offsets);
    }


    private static IReadOnlyList<ModuleInfo> CreateModules() =>
    new[]
    {
        new ModuleInfo { Name = "Kernel32", BaseAddress = 0x10000000 },
        new ModuleInfo { Name = "User32",   BaseAddress = 0x20000000 },
        new ModuleInfo { Name = "Game",     BaseAddress = 0x30000000 }
    };

    private static PointerBitLayout CreateLayout() =>
        new PointerBitLayout(
            maxModuleIndex: 2,
            maxModuleOffset: 1024,
            maxLevel: 5,
            maxOffset: 2048
        );
}
