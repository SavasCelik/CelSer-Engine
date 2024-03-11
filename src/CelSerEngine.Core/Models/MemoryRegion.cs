using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Core.Models;
public class MemoryRegion
{
    public IntPtr BaseAddress { get; set; }
    public ulong MemorySize { get; set; }
    public bool InModule { get; set; }
    public bool ValidPointerRange { get; set; }
}
