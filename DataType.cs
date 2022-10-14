using System;
using System.ComponentModel.DataAnnotations;
using CelSerEngine.Extensions;

namespace CelSerEngine
{
    public enum EnumDataType
    {
        [Display(Name = "2 Bytes")]
        Short,
        [Display(Name = "4 Bytes")]
        Integer,
        [Display(Name = "8 Bytes")]
        Long,
        [Display(Name = "Float")]
        Float,
        [Display(Name = "Double")]
        Double,
    }

    public class DataType
    {
        public Type Type { get; set; }
        public EnumDataType EnumType { get; set; }

        private DataType(EnumDataType type)
        {
            EnumType = type;
            Type = type switch
            {
                EnumDataType.Short => typeof(short),
                EnumDataType.Integer => typeof(int),
                EnumDataType.Long => typeof(long),
                EnumDataType.Float => typeof(float),
                EnumDataType.Double => typeof(double),
                _ => typeof(int)
            };
        }

        public string Name => EnumType.GetDisplayName();

        private static DataType[] dataTypes = new[]
            {
                new DataType(EnumDataType.Short),
                new DataType(EnumDataType.Integer),
                new DataType(EnumDataType.Long),
                new DataType(EnumDataType.Float),
                new DataType(EnumDataType.Double)
            };

        public static DataType[] GetDataTypes2 => dataTypes;
        public static EnumDataType[] GetDataTypes => Enum.GetValues<EnumDataType>();

        public static DataType GetDataType<T>()
        {
            if (typeof(T) == typeof(int))
            {
                return dataTypes[1];
            }

            throw new NotImplementedException();
        }
    }
}