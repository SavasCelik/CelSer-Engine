namespace CelSerEngine.Core.Scanners;

public interface IResultStorage : IDisposable
{
    void Save(int level, int moduleIndex, IntPtr baseOffset, ReadOnlySpan<IntPtr> offsets);
    List<ResultPointer> GetResults();
    int GetResultsCount();
}
