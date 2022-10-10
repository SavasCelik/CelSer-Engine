using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine
{
    public static class ByteArrayExtension
    {
        public static string ValueToString(this byte[] bytes, EnumDataType enumDataType)
        {
            return enumDataType switch
            {
                EnumDataType.Short => BitConverter.ToInt16(bytes).ToString(),
                EnumDataType.Integer => BitConverter.ToInt32(bytes).ToString(),
                EnumDataType.Float => BitConverter.ToSingle(bytes).ToString(),
                EnumDataType.Double => BitConverter.ToDouble(bytes).ToString(),
                EnumDataType.Long => BitConverter.ToInt64(bytes).ToString(),
                _ => BitConverter.ToInt32(bytes).ToString(),
            };
        }

        public static byte[] StringToValue(this string value, EnumDataType enumDataType)
        {
            return enumDataType switch
            {
                EnumDataType.Short => BitConverter.GetBytes(short.Parse(value)),
                EnumDataType.Integer => BitConverter.GetBytes(int.Parse(value)),
                EnumDataType.Float => BitConverter.GetBytes(float.Parse(value)),
                EnumDataType.Double => BitConverter.GetBytes(double.Parse(value)),
                EnumDataType.Long => BitConverter.GetBytes(long.Parse(value)),
                _ => BitConverter.GetBytes(int.Parse(value))
            };
        }

        public static object StringToObject(this string value, EnumDataType enumDataType)
        {
            var obj = new object();

            if (EnumDataType.Short == enumDataType)
            {
                obj = short.Parse(value);
            }
            else if (EnumDataType.Integer == enumDataType)
            {
                obj = int.Parse(value);
            }
            else if (EnumDataType.Float == enumDataType)
            {
                obj = float.Parse(value);
            }
            else if (EnumDataType.Double == enumDataType)
            {
                obj = double.Parse(value);
            }
            else if (EnumDataType.Long == enumDataType)
            {
                obj = long.Parse(value);
            }


            return obj;
        }
    }
}
