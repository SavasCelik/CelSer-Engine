using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scanners.Serialization;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.SerializationTests;

public sealed class Pointer7BitReadWriteTests
{
    [Fact]
    public void WriteThenRead_RoundTrip_ReturnsOriginalValues()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = CreateLayout();
        var modules = CreateModules();

        var writer = new Pointer7BitWriter(stream, layout);
        var reader = new Pointer7BitReader(stream, layout, modules);

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

        var writer = new Pointer7BitWriter(stream, layout);
        var reader = new Pointer7BitReader(stream, layout, modules);

        var offsets = new IntPtr[] { 42, 452 };

        // Act
        writer.Write(
            level: offsets.Length - 1,
            moduleIndex: 0,
            baseOffset: -512,
            offsets: offsets
        );

        stream.Position = 0;

        var pointer = reader.Read();

        // Assert
        Assert.Equal("Kernel32", pointer.ModuleName);
        Assert.Equal(-512, pointer.BaseOffset);
        Assert.Equal(offsets, pointer.Offsets);
    }

    [Fact]
    public void WriteThenRead_MultiByte7BitValues_RoundTripsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        var layout = CreateLayout();
        var modules = CreateModules();

        var writer = new Pointer7BitWriter(stream, layout);
        var reader = new Pointer7BitReader(stream, layout, modules);

        var offsets = new IntPtr[] { 200, 300, 400 }; // >127 requires multi-byte

        // Act
        writer.Write(
            level: offsets.Length - 1,
            moduleIndex: 2,
            baseOffset: 1000,
            offsets: offsets
        );

        stream.Position = 0;

        var pointer = reader.Read();

        // Assert
        Assert.Equal("Game", pointer.ModuleName);
        Assert.Equal(1000, pointer.BaseOffset);
        Assert.Equal(offsets, pointer.Offsets);
    }

    [Fact]
    public void WriteThenRead_MaxSupportedLevel_RoundTripsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        const int maxLevel = IPointerLayout.MaxSupportedLevel;

        var layout = new Pointer7BitLayout(
            maxModuleIndex: 1,
            maxModuleOffset: 1024,
            maxLevel: maxLevel,
            maxOffset: maxLevel
        );

        var modules = CreateModules();
        var offsets = Enumerable.Range(0, maxLevel).Select(i => (IntPtr)i).ToArray();

        var writer = new Pointer7BitWriter(stream, layout);
        var reader = new Pointer7BitReader(stream, layout, modules);

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
        Assert.Equal("Kernel32", pointer.ModuleName);
        Assert.Equal(0, pointer.BaseOffset);
    }

    private static IReadOnlyList<ModuleInfo> CreateModules() =>
        [
            new ModuleInfo { Name = "Kernel32", BaseAddress = 0x10000000 },
            new ModuleInfo { Name = "User32",   BaseAddress = 0x20000000 },
            new ModuleInfo { Name = "Game",     BaseAddress = 0x30000000 }
        ];

    private static Pointer7BitLayout CreateLayout() =>
        new(
            maxModuleIndex: 127,
            maxModuleOffset: 1024,
            maxLevel: 5,
            maxOffset: 1024
        );
}
