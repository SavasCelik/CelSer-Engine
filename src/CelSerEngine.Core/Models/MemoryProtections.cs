namespace CelSerEngine.Core.Models;

[Flags]
public enum MemoryProtections
{
    None = 0,
    Writable = 1 << 0,
    Executable = 1 << 1,
    CopyOnWrite = 1 << 2,
}
