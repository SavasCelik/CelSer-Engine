namespace CelSerEngine.Core.Scanners;

public class InMemoryStorage : IResultStorage
{
    private List<ResultPointer> _results = [];

    public void Save(int level, int moduleIndex, IntPtr baseOffset, ReadOnlySpan<IntPtr> offsets)
    {
        _results.Add(new ResultPointer { Level = level, ModuleIndex = moduleIndex, Offset = baseOffset, TempResults = offsets.ToArray() });
    }

    public List<ResultPointer> GetResults() => _results;

    public void Dispose() => _results.Clear();
    public int GetResultsCount() => _results.Count;
}
