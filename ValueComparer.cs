using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CelSerEngine.NativeCore;

namespace CelSerEngine
{
    public class ValueComparer<T> : IVectorComparer where T : struct
    {
        private readonly ScanConstraint _scanConstraint;
        private readonly int _sizeOfT;


        public ValueComparer(ScanConstraint scanConstraint)
        {
            _scanConstraint = scanConstraint;
            _sizeOfT = Marshal.SizeOf(default(T));
        }

        public bool CompareTo(object bytes)
        {
            return _scanConstraint.ScanContraintType switch
            {
                ScanContraintType.ExactValue => _scanConstraint.ValueObj.Equals(bytes),
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
                    var valueObject = bufferValue.ConvertBytesToObject<T>();

                    if (CompareTo(valueObject))
                    {
                        yield return new ValueAddress(virtualMemoryPage.Page.BaseAddress, i, bufferValue.ByteArrayToObject(_scanConstraint.DataType.EnumType), _scanConstraint.DataType.EnumType);
                    }
                }
            }
        }

        public static IVectorComparer CreateVectorComparer(ScanConstraint scanConstraint)
        {
            return scanConstraint.DataType.EnumType switch
            {
                EnumDataType.Short => new ValueComparer<short>(scanConstraint),
                EnumDataType.Integer => new ValueComparer<int>(scanConstraint),
                EnumDataType.Float => new ValueComparer<float>(scanConstraint),
                EnumDataType.Double => new ValueComparer<double>(scanConstraint),
                EnumDataType.Long => new ValueComparer<long>(scanConstraint),
                _ => new ValueComparer<int>(scanConstraint)
            };
        }
    }
}
