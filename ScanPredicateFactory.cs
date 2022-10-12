using CelSerEngine.NativeCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine
{
    public static class ScanPredicateFactory
    {
        public static Func<ValueAddress, bool> GetScanConstraintPredicate(ScanConstraint scanConstraint)
        {
            return scanConstraint.ScanContraintType switch 
            {
                ScanContraintType.ExactValue => (valueAdress) => (dynamic)valueAdress.Value == (dynamic)scanConstraint.ValueObj,
                ScanContraintType.SmallerThan => (valueAdress) => (dynamic)valueAdress.Value < (dynamic)scanConstraint.ValueObj,
                ScanContraintType.BiggerThan => (valueAdress) => (dynamic)valueAdress.Value > (dynamic)scanConstraint.ValueObj,
                _ => throw new NotImplementedException("Predicate not implemented")
            };
        }
    }
}
