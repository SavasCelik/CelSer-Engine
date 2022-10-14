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
            return scanConstraint.DataType.EnumType switch
            {
                EnumDataType.Short => new VectorComparer<short>(scanConstraint),
                EnumDataType.Integer => new VectorComparer<int>(scanConstraint),
                EnumDataType.Float => new VectorComparer<float>(scanConstraint),
                EnumDataType.Double => new VectorComparer<double>(scanConstraint),
                EnumDataType.Long => new VectorComparer<long>(scanConstraint),
                _ => new VectorComparer<int>(scanConstraint)
            };
        }
    }
}
