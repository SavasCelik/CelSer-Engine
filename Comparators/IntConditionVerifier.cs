using System;

namespace CelSerEngine.Comparers;

public static class IntConditionVerifier
{
    public static bool MeetsCondition(byte[] lhs, byte[] rhs, ScanContraintType scanContraintType)
    {
        var lhsVal = BitConverter.ToUInt32(lhs);
        var rhsVal = BitConverter.ToUInt32(rhs);
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
