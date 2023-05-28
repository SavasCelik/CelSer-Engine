using System.Globalization;
using System.Numerics;

namespace CelSerEngine.Core.Extensions;

public static class StringExtension
{
    public static T ParseToStruct<T>(this string stringValue)
        where T : INumber<T>
    {
        return T.Parse(stringValue, CultureInfo.InvariantCulture);
    }
}
