using System.Buffers;

namespace CelSerEngine.Core.Scanners;

public class PathQueueElement : IDisposable
{
    private static ArrayPool<IntPtr> s_intPtrArrayPool = ArrayPool<IntPtr>.Shared;
    private static ArrayPool<UIntPtr> s_uIntPtrArrayPool = ArrayPool<UIntPtr>.Shared;
    public IntPtr[] TempResults { get; set; }
    public UIntPtr[] ValueList { get; set; }
    public IntPtr ValueToFind { get; set; }
    public int StartLevel { get; set; }

    private bool _returned;

    public PathQueueElement(int maxLevel)
    {
        TempResults = s_intPtrArrayPool.Rent(maxLevel);
        ValueList = s_uIntPtrArrayPool.Rent(maxLevel);
    }

    public void Dispose()
    {
        if (_returned) return;
        _returned = true;
        s_intPtrArrayPool.Return(TempResults);
        s_uIntPtrArrayPool.Return(ValueList);
        TempResults = null!;
        ValueList = null!;
    }
}
