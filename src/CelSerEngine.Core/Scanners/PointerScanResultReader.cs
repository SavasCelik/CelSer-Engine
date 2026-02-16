using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scanners.Serialization;
using System.Diagnostics;

namespace CelSerEngine.Core.Scanners;

public sealed class PointerScanResultReader : IDisposable
{
    private sealed record PointerFileInfo(int StartIndex, int Count, IPointerReader Reader);
    private sealed record PointerScanResultMetaData(int TotalItemCount, int MaxModuleIndex, uint MaxModuleOffset, int MaxLevel, int MaxOffset);

    public int TotalItemCount => _metaData.TotalItemCount;
    private readonly PointerScanResultMetaData _metaData;
    private readonly List<PointerFileInfo> _pointerFiles;
    private readonly List<ModuleInfo> _modules;

    public PointerScanResultReader(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        if (!File.Exists(fileName))
            throw new FileNotFoundException($"File not found: {fileName}");

        _modules = [];
        _metaData = GetMetaData(fileName);
        _pointerFiles = [];
        GatherFiles(fileName);
    }

    /// <summary>
    /// Reads a contiguous range of pointers starting at <paramref name="startIndex"/>.
    /// <para>
    /// Pointers are stored across multiple underlying files. This method transparently
    /// reads from one or more files as needed to satisfy the request.
    /// </para>
    /// </summary>
    /// <param name="startIndex">
    /// The zero-based start index of the first pointer to read.
    /// </param>
    /// <param name="amount">
    /// The maximum number of pointers to read.
    /// </param>
    /// <returns>
    /// An array containing up to <paramref name="amount"/> pointers starting at
    /// <paramref name="startIndex"/>.  
    /// If fewer pointers are available than requested, the returned array
    /// will contain only the pointers that could be read.
    /// </returns>
    public Pointer[] ReadPointers(int startIndex, int amount)
    {
        if (amount <= 0)
            return [];

        var pointers = new Pointer[amount];
        var added = 0;
        var endIndex = startIndex + amount;

        foreach (var pointerFile in _pointerFiles)
        {
            if (endIndex <= pointerFile.StartIndex || startIndex >= pointerFile.StartIndex + pointerFile.Count)
                continue;

            var readIndex = startIndex + added;
            var fileStartIndex = readIndex - pointerFile.StartIndex;

            Debug.Assert(fileStartIndex >= 0);
            Debug.Assert(fileStartIndex < pointerFile.Count);

            var remaining = amount - added;
            var count = Math.Min(pointerFile.Count - fileStartIndex, remaining);
            pointerFile.Reader.ReadExactly(pointers.AsSpan(added, count), fileStartIndex);
            added += count;

            if (added == amount)
                break;
        }

        return pointers[..added];
    }

    private PointerScanResultMetaData GetMetaData(string fileName)
    {
        using var reader = new BinaryReader(File.OpenRead(fileName));
        var magic = reader.ReadUInt64();

        if (magic != PointerScanner2.Magic)
        {
            throw new InvalidDataException($"File is corrupted.");
        }

        var scanVersion = reader.ReadUInt32();

        if (scanVersion != PointerScanner2.Version)
        {
            throw new InvalidDataException($"Invalid file header. Expected version {PointerScanner2.Version}, found {scanVersion} in '{fileName}'.");
        }

        var modulesCount = reader.ReadInt32();
        _modules.Capacity = modulesCount;

        for (var i = 0; i < modulesCount; i++)
        {
            var shortName = reader.ReadString();
            var baseAddress = reader.ReadInt64();
            _modules.Add(new ModuleInfo { Name = shortName, BaseAddress = new IntPtr(baseAddress) });
        }

        var maxModuleOffset = reader.ReadUInt32();
        var maxLevel = reader.ReadInt32();
        var maxOffset = reader.ReadInt32();
        var totalItemCount = reader.ReadInt32();

        return new PointerScanResultMetaData(totalItemCount, modulesCount, maxModuleOffset, maxLevel, maxOffset);
    }

    private void GatherFiles(string fileName)
    {
        var directory = Path.GetDirectoryName(fileName)!;
        var workerFileNames = Path.GetFileName(fileName) + ".*";
        var filesPaths = Directory.EnumerateFiles(directory, workerFileNames).Where(x => x != fileName).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        var pointerBitLayout = new PointerBitLayout(_metaData.MaxModuleIndex, _metaData.MaxModuleOffset, _metaData.MaxLevel, _metaData.MaxOffset);
        var startIndex = 0;

        foreach (var filePath in filesPaths)
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Length == 0)
                continue;

            var reader = new PointerBitReader(File.OpenRead(filePath), pointerBitLayout, _modules);
            var pointerCount = (int)(reader.BaseStream.Length / pointerBitLayout.EntrySizeInBytes);
            _pointerFiles.Add(new PointerFileInfo(startIndex, pointerCount, reader));

            startIndex += pointerCount;
        }
    }

    public void Dispose()
    {
        foreach (var pointerFile in _pointerFiles)
        {
            pointerFile.Reader.Dispose();
        }
    }
}
