namespace CelSerEngine.Core.Extensions;

public static class IntPtrExtension
{
    public static bool InRange(this IntPtr val, IntPtr min, IntPtr max)
    {
        return val >= min && val <= max;
    }
}
