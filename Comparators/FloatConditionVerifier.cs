using System;

namespace CelSerEngine.Comparers;
public static class FloatConditionVerifier
{
    public static bool MeetsCondition(byte[] lhs, byte[] rhs, ScanContraintType scanContraintType)
    {
        var lhsVal = BitConverter.ToSingle(lhs);
        var rhsVal = BitConverter.ToSingle(rhs);
        return scanContraintType switch
        {
            ScanContraintType.ExactValue => lhsVal == rhsVal,
            ScanContraintType.BiggerThan => lhsVal > rhsVal,
            ScanContraintType.SmallerThan => lhsVal < rhsVal,
            ScanContraintType.UnknownInitialValue => true,
            ScanContraintType.ValueBetween => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }
}
