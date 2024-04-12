using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;

public partial class Counter : ComponentBase
{
    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    [Inject]
    public INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private MainWindow? MainWindow { get; set; }

    private IJSObjectReference? _module;

    protected string Message { get; set; } = "Hellow";

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "/js/main-window.js");
            await _module.InvokeVoidAsync("ready");
        }
    }

    private int counter = 0;

    private IList<IMemorySegment> _memorySegments;

    private async Task OpenSelectProcess()
    {
        var process = Process.GetProcessesByName("chrome").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, "1");
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        _memorySegments = comparer.GetMatchingMemorySegments(virtualMemoryRegions, null);
        selectedProcess.Dispose();
        await _module.InvokeVoidAsync("applyData", JsonSerializer.Serialize(_memorySegments.Take(1_000_000).Select(x => new { Address = x.Address.ToString("X"), x.Value })));
        //MainWindow.OpenProcessSelector();
    }

    private async Task NextScan()
    {
        var process = Process.GetProcessesByName("chrome").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, "1");
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        NativeApi.UpdateAddresses(pHandle, _memorySegments);
        var passedMemorySegments = new List<IMemorySegment>();

        for (var i = 0; i < _memorySegments.Count; i++)
        {
            if (ValueComparer.MeetsTheScanConstraint(_memorySegments[i].Value, scanConstraint.UserInput, scanConstraint))
                passedMemorySegments.Add(_memorySegments[i]);
        }

        selectedProcess.Dispose();
        await _module.InvokeVoidAsync("applyData", JsonSerializer.Serialize(passedMemorySegments.Select(x => new { Address = x.Address.ToString("X"), x.Value })));
        //MainWindow.OpenProcessSelector();
    }
}
