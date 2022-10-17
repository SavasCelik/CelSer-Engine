using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CelSerEngine.Extensions;
using CelSerEngine.NativeCore;

namespace CelSerEngine.Comparators
{
    public class ValueComparer : IScanComparer
    {
        private readonly ScanConstraint _scanConstraint;
        private dynamic _userInput;
        private readonly int _sizeOfT;

        public ValueComparer(ScanConstraint scanConstraint)
        {
            _scanConstraint = scanConstraint;
            _userInput = scanConstraint.ValueObj;
            _sizeOfT = scanConstraint.GetSize();
        }

        public static bool CompareDataByScanContraintType(dynamic lhs, dynamic rhs, ScanCompareType scanContraintType)
        {
            if (!((Type)lhs.GetType()).IsValueType)
                throw new ArgumentException("lhs must be a ValueType (struct)");

            if (!((Type)rhs.GetType()).IsValueType)
                throw new ArgumentException("rhs must be a ValueType (struct)");

            return scanContraintType switch
            {
                ScanCompareType.ExactValue => lhs == rhs,
                ScanCompareType.SmallerThan => lhs < rhs,
                ScanCompareType.BiggerThan => lhs > rhs,
                _ => throw new NotImplementedException("Not implemented")
            };
        }

        public IEnumerable<ValueAddress> GetMatchingValueAddresses(ICollection<VirtualMemoryPage> virtualMemoryPages)
        {
            foreach (var virtualMemoryPage in virtualMemoryPages)
            {
                for (var i = 0; i < (int)virtualMemoryPage.Page.RegionSize; i += _sizeOfT)
                {
                    if (i + _sizeOfT > (int)virtualMemoryPage.Page.RegionSize)
                    {
                        break;
                    }
                    var bufferValue = virtualMemoryPage.Bytes.AsSpan().Slice(i, _sizeOfT).ToArray();
                    var valueObject = bufferValue.ByteArrayToObject(_scanConstraint.ScanDataType);

                    if (CompareDataByScanContraintType(valueObject, _userInput, _scanConstraint.ScanCompareType))
                    {
                        yield return new ValueAddress(virtualMemoryPage.Page.BaseAddress, i, bufferValue.ByteArrayToObject(_scanConstraint.ScanDataType), _scanConstraint.ScanDataType);
                    }
                }
            }
        }
    }
}
