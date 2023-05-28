using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Extensions;

public static class ByteArrayExtension
{
    public static string ToScanDataTypeString(this byte[] byteArray, ScanDataType scanDataType)
    {
        return scanDataType switch
        {
            ScanDataType.Short => BitConverter.ToInt16(byteArray).ToString(),
            ScanDataType.Integer => BitConverter.ToInt32(byteArray).ToString(),
            ScanDataType.Float => BitConverter.ToSingle(byteArray).ToString(),
            ScanDataType.Double => BitConverter.ToDouble(byteArray).ToString(),
            ScanDataType.Long => BitConverter.ToInt64(byteArray).ToString(),
            _ => throw new NotImplementedException("")
        };
    }

    public static string ToScanDataTypeString(this Span<byte> byteArray, ScanDataType scanDataType)
    {
        return scanDataType switch
        {
            ScanDataType.Short => BitConverter.ToInt16(byteArray).ToString(),
            ScanDataType.Integer => BitConverter.ToInt32(byteArray).ToString(),
            ScanDataType.Float => BitConverter.ToSingle(byteArray).ToString(),
            ScanDataType.Double => BitConverter.ToDouble(byteArray).ToString(),
            ScanDataType.Long => BitConverter.ToInt64(byteArray).ToString(),
            _ => throw new NotImplementedException("")
        };
    }
}
