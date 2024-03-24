namespace CelSerEngine.Core.Scanners;

internal class PathQueueElement
{
    public IntPtr[] TempResults { get; set; }
    public UIntPtr[] ValueList { get; set; }
    public IntPtr ValueToFind { get; set; }
    public int StartLevel { get; set; }

    public PathQueueElement(int maxLevel)
    {
        TempResults = new IntPtr[maxLevel + 2];
        ValueList = new UIntPtr[maxLevel + 2];
    }
}
