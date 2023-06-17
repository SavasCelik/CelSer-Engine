namespace CelSerEngine.Core.Models;

public struct ProcessModuleInfo
{
    public IntPtr BaseAddress { get; set; }
    public uint Size { get; set; }

    public ProcessModuleInfo(IntPtr baseAddress, uint size)
    {
        BaseAddress = baseAddress;
        Size = size;
    }
}
