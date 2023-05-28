using CelSerEngine.Core.Models;
using System.Globalization;
using System.Numerics;

namespace CelSerEngine.Core.Extensions;

public static class StringExtension
{
    public static dynamic ToPrimitiveDataType(this string value, ScanDataType scanDataType)
    {
        if (ScanDataType.Short == scanDataType)
        {
            return short.Parse(value);
        }
        
        if (ScanDataType.Integer == scanDataType)
        {
            return int.Parse(value);
        }
        
        if (ScanDataType.Float == scanDataType)
        {
            return float.Parse(value);
        }
        
        if (ScanDataType.Double == scanDataType)
        {
            return double.Parse(value);
        }

        if (ScanDataType.Long == scanDataType)
        {
            return long.Parse(value);
        }

        throw new ArgumentOutOfRangeException($"Method ToPrimitiveDataType has no conversion for {scanDataType.GetDisplayName()}");
    }

    public static T ParseToINumberT<T>(this string stringValue) where T : INumber<T>
    {
        return T.Parse(stringValue, CultureInfo.InvariantCulture);
    }
}
