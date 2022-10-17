using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Comparators
{
    public static class ComparerFactory
    {
        public static IScanComparer CreateVectorComparer(ScanConstraint scanConstraint)
        {
            return scanConstraint.ScanDataType switch
            {
                ScanDataType.Short => new VectorComparer<short>(scanConstraint),
                ScanDataType.Integer => new VectorComparer<int>(scanConstraint),
                ScanDataType.Float => new VectorComparer<float>(scanConstraint),
                ScanDataType.Double => new VectorComparer<double>(scanConstraint),
                ScanDataType.Long => new VectorComparer<long>(scanConstraint),
                _ => new VectorComparer<int>(scanConstraint)
            };
        }
    }
}
