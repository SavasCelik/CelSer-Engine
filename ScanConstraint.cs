using System;
using System.ComponentModel.DataAnnotations;
using CelSerEngine.Extensions;

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

        public int GetSize()
        {
            return ScanDataType switch
            {
                ScanDataType.Short => sizeof(short),
                ScanDataType.Integer => sizeof(int),
                ScanDataType.Float => sizeof(float),
                ScanDataType.Double => sizeof(double),
                ScanDataType.Long => sizeof(long),
                _ => throw new Exception($"Type: {ScanDataType} is not supported")
            };
        }

        public bool Compare(byte[] bytes)
        {
            return ScanDataType switch
            {
                //EnumDataType.Integer => IntConditionVerifier.MeetsCondition(bytes, Value, ScanContraintType),
                //EnumDataType.Float => FloatConditionVerifier.MeetsCondition(bytes, Value, ScanContraintType),
                _ => false
            };
        }
    }
}
