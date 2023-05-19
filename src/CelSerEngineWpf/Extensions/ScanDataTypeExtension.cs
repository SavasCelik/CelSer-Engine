using CelSerEngine.Models;
using System;

namespace CelSerEngine.Extensions
{
    public static class ScanDataTypeExtension
    {
        public static int GetPrimitiveSize(this ScanDataType scanDataType)
        {
            return scanDataType switch
            {
                ScanDataType.Short => sizeof(short),
                ScanDataType.Integer => sizeof(int),
                ScanDataType.Float => sizeof(float),
                ScanDataType.Double => sizeof(double),
                ScanDataType.Long => sizeof(long),
                _ => throw new Exception($"Type: {scanDataType} is not supported")
            };
        }
    }
}
