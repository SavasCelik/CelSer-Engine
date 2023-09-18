using CelSerEngine.Core.Native;
using System.Diagnostics;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.Native;

public class NativeApiTests
{
    [Fact]
    public void OpenProcess_CurrentProcessName_ReturnValidHandle()
    {
        var nativeApi = CreateNativeApi();
        var currentProcess = Process.GetCurrentProcess();

        var actualProcessHandle = nativeApi.OpenProcess(currentProcess.ProcessName);

        Assert.NotEqual(IntPtr.Zero, actualProcessHandle);
    }

    [Fact]
    public void OpenProcess_CurrentProcessId_ReturnValidHandle()
    {
        var nativeApi = CreateNativeApi();
        var currentProcess = Process.GetCurrentProcess();

        var actualProcessHandle = nativeApi.OpenProcess(currentProcess.Id);

        Assert.NotEqual(IntPtr.Zero, actualProcessHandle);
    }

    [Fact]
    public void GetProcessMainModule_CurrentProcessId_ReturnCorrectModuleName()
    {
        var nativeApi = CreateNativeApi();
        var currentProcess = Process.GetCurrentProcess();

        var actualMainModule = nativeApi.GetProcessMainModule(currentProcess.Id);

        Assert.Equal(currentProcess.MainModule!.ModuleName, actualMainModule.Name);
    }

    [Fact]
    public void GetProcessMainModule_CurrentProcessId_ReturnCorrectModuleSize()
    {
        var nativeApi = CreateNativeApi();
        var currentProcess = Process.GetCurrentProcess();

        var actualMainModule = nativeApi.GetProcessMainModule(currentProcess.Id);

        Assert.Equal(currentProcess.MainModule!.ModuleMemorySize, (int)actualMainModule.Size);
    }

    [Fact]
    public void GetProcessMainModule_CurrentProcessId_ReturnCorrectBaseAddress()
    {
        var nativeApi = CreateNativeApi();
        var currentProcess = Process.GetCurrentProcess();

        var actualMainModule = nativeApi.GetProcessMainModule(currentProcess.Id);

        Assert.Equal(currentProcess.MainModule!.BaseAddress, actualMainModule.BaseAddress);
    }

    private static NativeApi CreateNativeApi() => new();
}
