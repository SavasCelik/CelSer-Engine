using System;
namespace CelSerEngine.Core.Scanners;

public struct Pointer2 : IEquatable<Pointer2>
{
    public IntPtr Address { get; set; }
    public IntPtr PoitingTo { get; set; }
    public IntPtr[] Offsets { get; set; }

    public Pointer2(nint address, nint poitingTo, nint[] offsets)
    {
        Address = address;
        PoitingTo = poitingTo;
        Offsets = offsets;
    }

    public override bool Equals(object? obj) => obj is Pointer2 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Address);
    public bool Equals(Pointer2 other) => Address.Equals(other.Address);
}
