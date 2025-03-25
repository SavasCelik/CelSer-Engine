using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Scanners;

public class PointerScanResultReader : IDisposable
{
    public int TotalItemCount { get; set; }
    public int MaxLevel { get; set; }

    private readonly BinaryReader _reader;
    private readonly List<ModuleInfo> _modules;

    public PointerScanResultReader(string fileName)
    {
        _modules = [];
        GatherMetaData(fileName);
        const int workerId = 1; // currently only single worker is supported
        _reader = new BinaryReader(File.OpenRead($"{fileName}.{workerId}"));
    }

    public IEnumerable<Pointer> ReadPointers(int startIndex, int amount)
    {
        var pointers = new List<Pointer>();
        var currentIndex = 0;

        while (_reader.BaseStream.Position < _reader.BaseStream.Length)
        {
            var moduleIndex = _reader.Read7BitEncodedInt();
            var baseOffset = _reader.Read7BitEncodedInt();
            var level = _reader.Read7BitEncodedInt();
            var offsets = new IntPtr[level];

            for (var i = 0; i < level; i++)
            {
                offsets[i] = new IntPtr(_reader.Read7BitEncodedInt());
            }

            if (startIndex <= currentIndex && currentIndex - startIndex < amount)
            {
                pointers.Add(new Pointer
                {
                    ModuleName = _modules[moduleIndex].Name,
                    BaseAddress = _modules[moduleIndex].BaseAddress,
                    BaseOffset = baseOffset,
                    Offsets = offsets
                });
            }

            currentIndex++;

            if (currentIndex - startIndex >= amount)
            {
                break;
            }
        }

        _reader.BaseStream.Position = 0;

        return pointers;
    }

    private void GatherMetaData(string fileName)
    {
        using var reader = new BinaryReader(File.OpenRead(fileName));
        var modulesCount = reader.ReadInt32();
        _modules.Capacity = modulesCount;

        for (var i = 0; i < modulesCount; i++)
        {
            var shortName = reader.ReadString();
            var baseAddress = reader.ReadInt64();
            _modules.Add(new ModuleInfo { Name = shortName, BaseAddress = new IntPtr(baseAddress) });
        }

        MaxLevel = reader.ReadInt32();
        TotalItemCount = reader.ReadInt32();
    }

    public void Dispose() => _reader.Dispose();
}
