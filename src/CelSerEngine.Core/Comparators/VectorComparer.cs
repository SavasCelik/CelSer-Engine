﻿using System.Numerics;
using System.Runtime.InteropServices;
using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators
{
    public class VectorComparer<T> : IScanComparer where T : struct, INumber<T>
    {
        private readonly ScanConstraint _scanConstraint;
        private readonly Vector<T> _userInputAsVector;
        private readonly int _sizeOfT;


        public VectorComparer(ScanConstraint scanConstraint)
        {
            _scanConstraint = scanConstraint;
            _userInputAsVector = new Vector<T>(scanConstraint.UserInput.ParseToStruct<T>());
            _sizeOfT = Marshal.SizeOf(default(T));
        }

        public int GetVectorSize()
        {
            return Vector<T>.Count;
        }

        public Vector<byte> CompareTo(ReadOnlySpan<byte> bytes)
        {
            return _scanConstraint.ScanCompareType switch
            {
                ScanCompareType.ExactValue => Vector.AsVectorByte(Vector.Equals(new Vector<T>(bytes), _userInputAsVector)),
                ScanCompareType.SmallerThan => Vector.AsVectorByte(Vector.LessThan(new Vector<T>(bytes), _userInputAsVector)),
                ScanCompareType.BiggerThan => Vector.AsVectorByte(Vector.GreaterThan(new Vector<T>(bytes), _userInputAsVector)),
                _ => throw new NotImplementedException("Not implemented")
            };
        }

        public IEnumerable<ProcessMemory> GetMatchingValueAddresses(IList<VirtualMemoryPage> virtualMemoryPages, IProgress<float> progressBarUpdater)
        {
            for (var pageIndex = 0; pageIndex < virtualMemoryPages.Count; pageIndex++)
            {
                var virtualMemoryPage = virtualMemoryPages[pageIndex];
                var remaining = (int)virtualMemoryPage.RegionSize % GetVectorSize();

                for (var i = 0; i < (int)virtualMemoryPage.RegionSize - remaining; i += Vector<byte>.Count)
                {
                    var splitBuffer = virtualMemoryPage.Bytes.AsSpan().Slice(i, Vector<byte>.Count);
                    var compareResult = CompareTo(splitBuffer);

                    if (!compareResult.Equals(Vector<byte>.Zero))
                    {
                        var desti = new byte[Vector<byte>.Count];
                        Vector.AsVectorByte(compareResult).CopyTo(desti);
                        for (var j = 0; j < Vector<byte>.Count; j += _sizeOfT)
                        {
                            if (compareResult[j] != 0)
                            {
                                var newIntPtr = (IntPtr)virtualMemoryPage.BaseAddress + i + j;
                                var myArry = virtualMemoryPage.Bytes.AsSpan().Slice(j + i, _sizeOfT).ToArray();

                                yield return 
                                    new ProcessMemory(
                                        virtualMemoryPage.BaseAddress, i + j,
                                        myArry.ByteArrayToObject(_scanConstraint.ScanDataType),
                                        _scanConstraint.ScanDataType);
                            }
                        }
                    }
                }

                var progress = (float)pageIndex * 100 / virtualMemoryPages.Count;
                progressBarUpdater?.Report(progress);
            }
        }
    }
}
