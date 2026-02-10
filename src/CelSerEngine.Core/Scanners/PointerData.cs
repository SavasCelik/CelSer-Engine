namespace CelSerEngine.Core.Scanners;

internal struct PointerData
{
    public IntPtr Address { get; set; }
    public StaticData? StaticData { get; set; }
}
