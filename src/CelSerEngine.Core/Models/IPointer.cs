namespace CelSerEngine.Core.Models;
public interface IPointer
{
    public IntPtr PointingTo { get; set; }
    public string ModuleName { get; set; }
    public List<IntPtr> Offsets { get; set; }
}
