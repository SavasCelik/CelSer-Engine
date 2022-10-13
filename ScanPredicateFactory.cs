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
                ScanContraintType.ExactValue => (valueAdress) => valueAdress.Value == scanConstraint.ValueObj,
                ScanContraintType.SmallerThan => (valueAdress) => valueAdress.Value < scanConstraint.ValueObj,
                ScanContraintType.BiggerThan => (valueAdress) => valueAdress.Value > scanConstraint.ValueObj,
                _ => throw new NotImplementedException("Predicate not implemented")
            };
        }
    }
}
