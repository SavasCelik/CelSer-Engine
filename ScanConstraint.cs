using System;

namespace CelSerEngine
{
    public class ScanConstraint
    {
        public byte[] Value { get; set; }
        public dynamic ValueObj { get; set; }
        public ScanDataType ScanDataType { get; set; }
        public ScanCompareType ScanCompareType { get; set; }

        public ScanConstraint(ScanCompareType scanCompareType, ScanDataType dataType)
        {
            ScanDataType = dataType;
            ScanCompareType = scanCompareType;
            Value = Array.Empty<byte>();
        }
    }
}
