namespace CelSerEngine.Core.Scanners;

public class ResultPointer
{
    public int ModuleIndex { get; set; }
    public IntPtr Offset { get; set; }
    public required IntPtr[] TempResults { get; set; }
    public int Level { get; set; }
}
