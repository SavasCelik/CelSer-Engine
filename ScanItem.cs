using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine
{
    public class ScanItem
    {
        public IntPtr Address { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
        public string? Previous { get; set; }
    }
}
