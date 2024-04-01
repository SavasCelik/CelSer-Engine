using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace CelSerEngine.Core.Scripting;

/// <summary>
/// The MemoryManager class provides functionality to read and write to a process's memory. 
/// </summary>
public class MemoryManager
{
    private readonly SafeProcessHandle _processHandle;
    private readonly INativeApi _nativeApi;

    /// <summary>
    /// Creates an instance of the MemoryManager class.
    /// </summary>
    /// <param name="processHandle">A pointer (<see cref="IntPtr"/>) to the target process handle</param>
    /// <param name="nativeApi">An instance of an object that implements the <see cref="INativeApi"/> interface.
    /// This provides the underlying mechanism to interact with system-level operations for reading and writing memory.</param>
    public MemoryManager(SafeProcessHandle processHandle, INativeApi nativeApi)
    {
        _processHandle = processHandle;
        _nativeApi = nativeApi;
    }

    /// <summary>
    /// Reads the memory content at the specified memory address
    /// </summary>
    /// <typeparam name="T">The return type <see cref="T"/> must be a value type struct</typeparam>
    /// <param name="memoryAddress">An integer representing the memory address from where the data needs to be read.</param>
    /// <returns>The result in the type specified by T</returns>
    public T ReadMemory<T>(int memoryAddress)
        where T : struct
    {
        var memoryAddressIntPtr = new IntPtr(memoryAddress); 
        var typeSize = Marshal.SizeOf(typeof(T));

        if (_nativeApi.TryReadVirtualMemory(_processHandle, memoryAddressIntPtr, (uint)typeSize, out var bytes))
            return default;
        
        IntPtr ptr = Marshal.AllocHGlobal(typeSize);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        var result = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);

        return result;
    }

    /// <summary>
    /// Writes a value to the memory at the specified memory address.
    /// </summary>
    /// <typeparam name="T">The new value's type</typeparam>
    /// <param name="memoryAddress">An integer representing the memory address where the data needs to be written to.</param>
    /// <param name="newValue">The value of type T that needs to be written to the specified memory address.</param>
    public void WriteMemory<T>(int memoryAddress, T newValue)
        where T : struct
    {
        var memoryAddressIntPtr = new IntPtr(memoryAddress);
        _nativeApi.WriteMemory(_processHandle, memoryAddressIntPtr, newValue);
    }
}
