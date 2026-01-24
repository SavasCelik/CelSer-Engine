namespace CelSerEngine.Core.Scanners;

public class FileStorage : IResultStorage
{
    private readonly FileStream _fileStream;
    private readonly BufferedStream _bufferedStream;
    private readonly BinaryWriter _binaryWriter;
    private int _count;

    public FileStorage(string fileName)
    {
        const int bufferSize = 15 * 1024 * 1024; // 15 MB buffer size before writing to disk
        _fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        _bufferedStream = new BufferedStream(_fileStream, bufferSize);
        _binaryWriter = new BinaryWriter(_bufferedStream);
    }

    public void Save(int level, int moduleIndex, IntPtr baseOffset, ReadOnlySpan<IntPtr> offsets)
    {
        _binaryWriter.Write7BitEncodedInt(moduleIndex);
        _binaryWriter.Write7BitEncodedInt(baseOffset.ToInt32());
        _binaryWriter.Write7BitEncodedInt(level + 1);
        foreach (var tempResult in offsets)
        {
            _binaryWriter.Write7BitEncodedInt(tempResult.ToInt32());
        }
        _count++;
    }

    public List<ResultPointer> GetResults() => [];

    public async ValueTask DisposeAsync()
    {
        await _binaryWriter.DisposeAsync();
        await _bufferedStream.DisposeAsync();
        await _fileStream.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public int GetResultsCount() => _count;
}
