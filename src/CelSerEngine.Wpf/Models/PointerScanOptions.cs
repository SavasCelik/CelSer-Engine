using System;

namespace CelSerEngine.Wpf.Models;
public class PointerScanOptions
{
    public ProcessAdapter ProcessAdapter { get; set; }
    public IntPtr SearchedAddress { get; set; }
    public int MaxLevel { get; set; }
    public int MaxOffset { get; set; }

}
