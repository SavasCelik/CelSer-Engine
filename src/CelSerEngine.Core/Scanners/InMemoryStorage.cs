namespace CelSerEngine.Core.Scanners;

public class InMemoryStorage : IResultStorage
{
    private List<ResultPointer> Results = [];

    public void Save(int level, int moduleIndex, IntPtr baseOffset, ReadOnlySpan<IntPtr> offsets)
    {
        Results.Add(new ResultPointer { Level = level, ModuleIndex = moduleIndex, Offset = baseOffset, TempResults = offsets.ToArray() });
    }

    public List<ResultPointer> GetResults() => Results;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
