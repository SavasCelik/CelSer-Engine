using System;

namespace CelSerEngine.Models
{
    public class ScanConstraint
    {
        public dynamic UserInput { get; set; }
        public ScanDataType ScanDataType { get; set; }
        public ScanCompareType ScanCompareType { get; set; }

        public ScanConstraint(ScanCompareType scanCompareType, ScanDataType dataType)
        {
            ScanDataType = dataType;
            ScanCompareType = scanCompareType;
            UserInput = "";
        }
    }
}
