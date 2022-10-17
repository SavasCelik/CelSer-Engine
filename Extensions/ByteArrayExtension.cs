using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Extensions
{
    public static class ByteArrayExtension
    {
        public static string ValueToString(this byte[] bytes, ScanDataType scanDataType)
        {
            return scanDataType switch
            {
                ScanDataType.Short => BitConverter.ToInt16(bytes).ToString(),
                ScanDataType.Integer => BitConverter.ToInt32(bytes).ToString(),
                ScanDataType.Float => BitConverter.ToSingle(bytes).ToString(),
                ScanDataType.Double => BitConverter.ToDouble(bytes).ToString(),
                ScanDataType.Long => BitConverter.ToInt64(bytes).ToString(),
                _ => BitConverter.ToInt32(bytes).ToString(),
            };
        }

        public static string ValueToString(this object obj, ScanDataType scanDataType)
        {
            return scanDataType switch
            {
                ScanDataType.Short => ((short)obj).ToString(),
                ScanDataType.Integer => ((int)obj).ToString(),
                ScanDataType.Float => ((float)obj).ToString(),
                ScanDataType.Double => ((double)obj).ToString(),
                ScanDataType.Long => ((long)obj).ToString(),
                _ => ((int)obj).ToString(),
            };
        }

        public static byte[] StringToValue(this string value, ScanDataType scanDataType)
        {
            return scanDataType switch
            {
                ScanDataType.Short => BitConverter.GetBytes(short.Parse(value)),
                ScanDataType.Integer => BitConverter.GetBytes(int.Parse(value)),
                ScanDataType.Float => BitConverter.GetBytes(float.Parse(value)),
                ScanDataType.Double => BitConverter.GetBytes(double.Parse(value)),
                ScanDataType.Long => BitConverter.GetBytes(long.Parse(value)),
                _ => BitConverter.GetBytes(int.Parse(value))
            };
        }

        public static object StringToObject(this string value, ScanDataType scanDataType)
        {
            var obj = new object();

            if (ScanDataType.Short == scanDataType)
            {
                obj = short.Parse(value);
            }
            else if (ScanDataType.Integer == scanDataType)
            {
                obj = int.Parse(value);
            }
            else if (ScanDataType.Float == scanDataType)
            {
                obj = float.Parse(value);
            }
            else if (ScanDataType.Double == scanDataType)
            {
                obj = double.Parse(value);
            }
            else if (ScanDataType.Long == scanDataType)
            {
                obj = long.Parse(value);
            }

            return obj;
        }

        public static object ByteArrayToObject(this byte[] byteArray, ScanDataType scanDataType)
        {
            if (ScanDataType.Short == scanDataType)
            {
                return BitConverter.ToInt16(byteArray);
            }
            if (ScanDataType.Integer == scanDataType)
            {
                return BitConverter.ToInt32(byteArray);
            }
            if (ScanDataType.Float == scanDataType)
            {
                return BitConverter.ToSingle(byteArray);
            }
            if (ScanDataType.Double == scanDataType)
            {
                return BitConverter.ToDouble(byteArray);
            }
            if (ScanDataType.Long == scanDataType)
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
