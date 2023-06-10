using System.Numerics;

namespace CelSerEngine.Core.UnitTests.Comparators;

public static class Util
{
    private static Random s_random = Random.Shared;

    public static T[] GenerateRandomValues<T>(int numValues, int min = 1, int max = 100) where T : struct
    {
        T[] values = new T[numValues];
        for (int g = 0; g < numValues; g++)
        {
            values[g] = GenerateSingleValue<T>(min, max);
        }

        return values;
    }

    public static T GenerateSingleValue<T>(int min = 1, int max = 100) where T : struct
    {
        var randomRange = s_random.Next(min, max);
        T value = unchecked((T)(dynamic)randomRange);
        return value;
    }

    public static T[] GenerateRandomValuesForVector<T>(int? numValues = null) where T : struct
    {
        int minValue = GetMinValue<T>();
        int maxValue = GetMaxValue<T>();
        return GenerateRandomValues<T>(numValues ?? Vector<T>.Count, minValue, maxValue);
    }

    internal static int GetMinValue<T>() where T : struct
    {
        if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
        {
            return int.MinValue;
        }
        else if (typeof(T) == typeof(byte))
        {
            return byte.MinValue;
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return sbyte.MinValue;
        }
        else if (typeof(T) == typeof(short))
        {
            return short.MinValue;
        }
        else if (typeof(T) == typeof(ushort))
        {
            return ushort.MinValue;
        }
        throw new NotSupportedException();
    }

    internal static int GetMaxValue<T>() where T : struct
    {
        if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
        {
            return int.MaxValue;
        }
        else if (typeof(T) == typeof(byte))
        {
            return byte.MaxValue;
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return sbyte.MaxValue;
        }
        else if (typeof(T) == typeof(short))
        {
            return short.MaxValue;
        }
        else if (typeof(T) == typeof(ushort))
        {
            return ushort.MaxValue;
        }
        throw new NotSupportedException();
    }
}