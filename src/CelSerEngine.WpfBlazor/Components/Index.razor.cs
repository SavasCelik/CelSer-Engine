using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace CelSerEngine.WpfBlazor.Components;

public partial class Index : ComponentBase
{
    [Inject]
    public INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private MainWindow? MainWindow { get; set; }

    private VirtualizedAgGrid<IMemorySegment> _virtualizedAgGridRef = default!;
    private List<IMemorySegment> _memorySegments = [];

    private ScanDataType _selectedScanDataType = ScanDataType.Integer;
    private ScanCompareType _selectedScanCompareType = ScanCompareType.ExactValue;

    private async Task OpenSelectProcess()
    {
        await _virtualizedAgGridRef.ShowScanningOverlay();
        var process = Process.GetProcessesByName("chrome").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, "1");
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        _memorySegments.Clear();
        _memorySegments.AddRange(comparer.GetMatchingMemorySegments(virtualMemoryRegions, null));
        selectedProcess.Dispose();
        await _virtualizedAgGridRef.ApplyDataAsync();
        //await _module.InvokeVoidAsync("applyData", JsonSerializer.Serialize(_memorySegments.Take(8).Select(x => new { Address = x.Address.ToString("X"), x.Value })), _memorySegments.Count);
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
        //MainWindow.OpenProcessSelector();
    }
}
