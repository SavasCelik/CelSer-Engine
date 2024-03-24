namespace CelSerEngine.Core.Scanners;

internal class ResultPointer
{
    public int ModuleIndex { get; set; }
    public IntPtr Offset { get; set; }
    public IntPtr[] TempResults { get; set; } = new IntPtr[PointerScanner2.MaxLevel];
    public int Level { get; set; }
}
