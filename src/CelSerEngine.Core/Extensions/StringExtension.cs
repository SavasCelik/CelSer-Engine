using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace CelSerEngine.Core.Extensions;

public static class StringExtension
{
    public static T ParseNumber<T>(this string stringValue)
        where T : INumber<T>
    {
        return T.Parse(stringValue, CultureInfo.InvariantCulture);
    }

    public static bool TryParseNumber<T>(this string stringValue, [MaybeNullWhen(false)] out T value)
        where T : INumber<T>
    {
        return T.TryParse(stringValue, CultureInfo.InvariantCulture, out value);
    }
}
