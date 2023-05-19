using CelSerEngine.Models;
using System;

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
    }
}
