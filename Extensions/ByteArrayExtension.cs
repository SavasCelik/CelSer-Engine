using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Extensions
{
    public static class ByteArrayExtension
    {
        public static string ValueToString(this byte[] bytes, ScanDataType enumDataType)
        {
            return enumDataType switch
            {
                ScanDataType.Short => BitConverter.ToInt16(bytes).ToString(),
                ScanDataType.Integer => BitConverter.ToInt32(bytes).ToString(),
                ScanDataType.Float => BitConverter.ToSingle(bytes).ToString(),
                ScanDataType.Double => BitConverter.ToDouble(bytes).ToString(),
                ScanDataType.Long => BitConverter.ToInt64(bytes).ToString(),
                _ => BitConverter.ToInt32(bytes).ToString(),
            };
        }

        public static string ValueToString(this object obj, ScanDataType enumDataType)
        {
            return enumDataType switch
            {
                ScanDataType.Short => ((short)obj).ToString(),
                ScanDataType.Integer => ((int)obj).ToString(),
                ScanDataType.Float => ((float)obj).ToString(),
                ScanDataType.Double => ((double)obj).ToString(),
                ScanDataType.Long => ((long)obj).ToString(),
                _ => ((int)obj).ToString(),
            };
        }

        public static byte[] StringToValue(this string value, ScanDataType enumDataType)
        {
            return enumDataType switch
            {
                ScanDataType.Short => BitConverter.GetBytes(short.Parse(value)),
                ScanDataType.Integer => BitConverter.GetBytes(int.Parse(value)),
                ScanDataType.Float => BitConverter.GetBytes(float.Parse(value)),
                ScanDataType.Double => BitConverter.GetBytes(double.Parse(value)),
                ScanDataType.Long => BitConverter.GetBytes(long.Parse(value)),
                _ => BitConverter.GetBytes(int.Parse(value))
            };
        }

        public static object StringToObject(this string value, ScanDataType enumDataType)
        {
            var obj = new object();

            if (ScanDataType.Short == enumDataType)
            {
                obj = short.Parse(value);
            }
            else if (ScanDataType.Integer == enumDataType)
            {
                obj = int.Parse(value);
            }
            else if (ScanDataType.Float == enumDataType)
            {
                obj = float.Parse(value);
            }
            else if (ScanDataType.Double == enumDataType)
            {
                obj = double.Parse(value);
            }
            else if (ScanDataType.Long == enumDataType)
            {
                obj = long.Parse(value);
            }


            return obj;
        }

        public static object ByteArrayToObject(this byte[] byteArray, ScanDataType enumDataType)
        {
            if (ScanDataType.Short == enumDataType)
            {
                return BitConverter.ToInt16(byteArray);
            }
            if (ScanDataType.Integer == enumDataType)
            {
                return BitConverter.ToInt32(byteArray);
            }
            if (ScanDataType.Float == enumDataType)
            {
                return BitConverter.ToSingle(byteArray);
            }
            if (ScanDataType.Double == enumDataType)
            {
                return BitConverter.ToDouble(byteArray);
            }
            if (ScanDataType.Long == enumDataType)
            {
                return BitConverter.ToInt64(byteArray);
            }

            throw new NotImplementedException("");
        }

        public static T ToType<T>(this byte[] bytes) where T : struct
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)BitConverter.ToInt32(bytes);
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)BitConverter.ToSingle(bytes);
            }

            if (typeof(T) == typeof(double))
            {
                return (T)(object)BitConverter.ToDouble(bytes);
            }

            throw new NotImplementedException("Not implemented");
        }
    }
}
