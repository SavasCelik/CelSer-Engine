using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using CelSerEngine.Comparers;

namespace CelSerEngine
{
    public enum ScanContraintType
    {
        [Display(Name = "Exact Value")]
        ExactValue,
        [Display(Name = "Bigger than...")]
        BiggerThan,
        [Display(Name = "Smaller than...")]
        SmallerThan,
        [Display(Name = "Value between...")]
        ValueBetween,
        [Display(Name = "Unknown initial value")]
        UnknownInitialValue
    }

    public class ScanConstraint
    {
        public byte[] Value { get; set; }
        public object ValueObj { get; set; }
        public DataType DataType { get; set; }
        public ScanContraintType ScanContraintType { get; set; }
        public string Name => ScanContraintType.GetDisplayName();

        private ScanConstraint(ScanContraintType scanContraintType, DataType? dataType = null)
        {
            DataType = dataType ?? DataType.GetDataTypes[1];
            ScanContraintType = scanContraintType;
            Value = Array.Empty<byte>();
        }

        private static ScanConstraint[] scanContraintTypes = new[]
        {
            new ScanConstraint(ScanContraintType.ExactValue),
            new ScanConstraint(ScanContraintType.BiggerThan),
            new ScanConstraint(ScanContraintType.SmallerThan),
            new ScanConstraint(ScanContraintType.ValueBetween),
            new ScanConstraint(ScanContraintType.UnknownInitialValue)
        };

        public static ScanConstraint[] GetScanContraintTypes => scanContraintTypes;

        public int GetSize()
        {
            return DataType.EnumType switch
            {
                EnumDataType.Short => sizeof(short),
                EnumDataType.Integer => sizeof(int),
                EnumDataType.Float => sizeof(float),
                EnumDataType.Double => sizeof(double),
                EnumDataType.Long => sizeof(long),
                _ => sizeof(int)
            };
        }

        public bool Comapare(byte[] bytes)
        {
            return DataType.EnumType switch
            {
                EnumDataType.Integer => IntConditionVerifier.MeetsCondition(bytes, Value, ScanContraintType),
                EnumDataType.Float => FloatConditionVerifier.MeetsCondition(bytes, Value, ScanContraintType),
                _ => false
            };
        }

        public int GetVectorSize()
        {
            return DataType.EnumType switch
            {
                EnumDataType.Short => Vector<short>.Count,
                EnumDataType.Integer => Vector<int>.Count,
                EnumDataType.Float => Vector<float>.Count,
                EnumDataType.Double => Vector<double>.Count,
                EnumDataType.Long => Vector<long>.Count,
                _ => Vector<int>.Count
            };
        }
    }
}
