namespace CelSerEngine.Models;

public class TrackedPointerScanItem : TrackedScanItem
{
    public Pointer Pointer { get; set; }

    public TrackedPointerScanItem(Pointer pointer) : base((ulong)pointer.PointingTo.ToInt64(), 0, 0, ScanDataType.Integer)
    {
        Pointer = pointer;
        DetermineAddressDisplayString();
    }

    public void DetermineAddressDisplayString()
    {
        AddressDisplayString = $"P->{Address:X}";
    }
}
