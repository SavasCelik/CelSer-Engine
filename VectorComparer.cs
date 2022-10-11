using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using CelSerEngine.NativeCore;

namespace CelSerEngine
{
    public class VectorComparer<T> : IVectorComparer where T : struct
    {
        private readonly ScanConstraint _scanConstraint;
        private readonly Vector<T> _userInputAsVector;
        private readonly int _sizeOfT;


        public VectorComparer(ScanConstraint scanConstraint)
        {
            _scanConstraint = scanConstraint;
            _userInputAsVector = new Vector<T>((T)scanConstraint.ValueObj);
            var bytes = new byte[15];
            _sizeOfT = Marshal.SizeOf(default(T));

            //var lol = new Vector<T>(bytes.AsSpan());
        }

        public int GetVectorSize()
        {
            return Vector<T>.Count;
        }

        public Vector<byte> ComapreTo(ReadOnlySpan<byte> bytes)
        {
            return _scanConstraint.ScanContraintType switch
            {
                ScanContraintType.ExactValue => Vector.AsVectorByte(Vector.Equals(_userInputAsVector, new Vector<T>(bytes))),
                _ => throw new NotImplementedException("Not implemented")
            };
        }

        public IEnumerable<ValueAddress> GetMatchingValueAddresses(MEMORY_BASIC_INFORMATION64 page, byte[] pageValues)
        {
            var remaining = (int)page.RegionSize % GetVectorSize();

            for (var i = 0; i < (int)page.RegionSize - remaining; i += Vector<byte>.Count)
            {
                var splitBuffer = pageValues.AsSpan().Slice(i, Vector<byte>.Count);
                var compareResult = ComapreTo(splitBuffer);

                if (!compareResult.Equals(Vector<byte>.Zero))
                {
                    var desti = new byte[Vector<byte>.Count];
                    Vector.AsVectorByte(compareResult).CopyTo(desti);
                    for (var j = 0; j < Vector<byte>.Count; j += _sizeOfT)
                    {
                        if (compareResult[j] != 0)
                        {
                            var newIntPtr = (IntPtr)page.BaseAddress + i + j;
                            var myArry = ConvertBytesToObject(pageValues.AsSpan().Slice(j+i, _sizeOfT).ToArray());
                            yield return new ValueAddress(page.BaseAddress, i + j, myArry, DataType.GetDataType<T>().EnumType);
                        }
                    }
                }

            }
        }


        public static IVectorComparer CreateVectorComparer(ScanConstraint scanConstraint)
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

        private object ConvertBytesToObject(byte[] bytes)
        {
            if (typeof(T) == typeof(int))
            {
                return BitConverter.ToInt32(bytes);
            }

            if (typeof(T) == typeof(float))
            {
                return BitConverter.ToSingle(bytes);
            }

            throw new NotImplementedException("Not implemented");
        }

    }
}
