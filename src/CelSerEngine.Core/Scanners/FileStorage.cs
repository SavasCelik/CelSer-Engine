using CelSerEngine.Core.Scanners.Serialization;

namespace CelSerEngine.Core.Scanners;

public class FileStorage : IResultStorage
{
    private readonly IPointerWriter _pointerWriter;
    private int _count;

    public FileStorage(string fileName, int maxModuleIndex, uint maxModuleOffset, int maxLevel, int maxOffset, bool useBitWriter = true)
    {
        const int bufferSize = 15 * 1024 * 1024; // 15 MB buffer size before writing to disk
        var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: bufferSize);
        
        if (useBitWriter)
        {
            var layout = new PointerBitLayout(maxModuleIndex, maxModuleOffset, maxLevel, maxOffset);
            _pointerWriter = new PointerBitWriter(fileStream, layout);
        }
        else
        {
            var layout = new Pointer7BitLayout(maxModuleIndex, maxModuleOffset, maxLevel, maxOffset);
            _pointerWriter = new Pointer7BitWriter(fileStream, layout);
        }

    }

    public void Save(int level, int moduleIndex, IntPtr baseOffset, ReadOnlySpan<IntPtr> offsets)
    {
        _pointerWriter.Write(level, moduleIndex, baseOffset.ToInt32(), offsets);
        _count++;
    }

    public List<ResultPointer> GetResults() => [];

    public int GetResultsCount() => _count;

    public void Dispose()
    {
        _pointerWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}
