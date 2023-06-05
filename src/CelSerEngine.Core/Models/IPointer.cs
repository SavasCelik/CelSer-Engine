namespace CelSerEngine.Core.Models;
public interface IPointer : IMemorySegment
{
    public IntPtr PointingTo { get; set; }
    public string ModuleName { get; set; }
    public IList<IntPtr> Offsets { get; set; }
}
