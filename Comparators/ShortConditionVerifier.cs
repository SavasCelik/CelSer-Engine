using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Comparers;

public class ShortConditionVerifier
{
    public static bool MeetsCondition(byte[] lhs, byte[] rhs, ScanContraintType scanContraintType)
    {
        var lhsVal = BitConverter.ToUInt16(lhs);
        var rhsVal = BitConverter.ToUInt16(rhs);
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
