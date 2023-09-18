namespace CelSerEngine.Core.Models;

public struct ProcessModuleInfo
{
    public string Name { get; }
    public IntPtr BaseAddress { get; }
    public uint Size { get; }

    public ProcessModuleInfo(string name, IntPtr baseAddress, uint size)
    {
        Name = name;
        BaseAddress = baseAddress;
        Size = size;
    }
}
