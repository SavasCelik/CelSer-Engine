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
    public class ValueComparer<T> : IScanComparer where T : struct
    {
        private readonly ScanConstraint _scanConstraint;
        private dynamic _userInput;
        private readonly int _sizeOfT;


        public ValueComparer(ScanConstraint scanConstraint)
        {
            _scanConstraint = scanConstraint;
            _userInput = scanConstraint.ValueObj;
            _sizeOfT = Marshal.SizeOf(default(T));
        }

        public bool CompareTo(T bytes)
        {
            return _scanConstraint.ScanContraintType switch
            {
                ScanContraintType.ExactValue => (dynamic)bytes == _userInput,
                ScanContraintType.SmallerThan => (dynamic)bytes < _userInput,
                ScanContraintType.BiggerThan => (dynamic)bytes > _userInput,
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
                    var valueObject = bufferValue.ToType<T>();

                    if (CompareTo(valueObject))
                    {
                        yield return new ValueAddress(virtualMemoryPage.Page.BaseAddress, i, bufferValue.ByteArrayToObject(_scanConstraint.DataType.EnumType), _scanConstraint.DataType.EnumType);
                    }
                }
            }
        }
    }
}
