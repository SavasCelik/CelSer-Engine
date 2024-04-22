using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;

namespace CelSerEngine.WpfBlazor.Components;

public partial class Index : ComponentBase
{
    [Inject]
    public INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private MainWindow? MainWindow { get; set; }

    private VirtualizedAgGrid<ScanResultItem> _virtualizedAgGridRef = default!;
    private List<ScanResultItem> _scanResultItems { get; set; } = [];

    private string _searchValue { get; set; } = string.Empty;
    private ScanDataType _selectedScanDataType { get; set; } = ScanDataType.Integer;
    private ScanCompareType _selectedScanCompareType { get; set; } = ScanCompareType.ExactValue;

    private async Task FirstScan()
    {
        await _virtualizedAgGridRef.ShowScanningOverlay();
        var process = Process.GetProcessesByName("SmallGame").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(_selectedScanCompareType, _selectedScanDataType, _searchValue);
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        _scanResultItems.Clear();
        _scanResultItems.AddRange(comparer.GetMatchingMemorySegments(virtualMemoryRegions, null).Select(x => new ScanResultItem(x)));
        selectedProcess.Dispose();
        await _virtualizedAgGridRef.ApplyDataAsync();
    }

    public async Task Enter(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
        {
            await FirstScan();
        }
    }

    private async Task OpenSelectProcess()
    {
        MainWindow.OpenProcessSelector();
    }

    private async Task NextScan()
    {
        var process = Process.GetProcessesByName("chrome").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, "1");
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        NativeApi.UpdateAddresses(pHandle, _scanResultItems);
        var passedMemorySegments = new List<IMemorySegment>();

        for (var i = 0; i < _scanResultItems.Count; i++)
        {
            if (ValueComparer.MeetsTheScanConstraint(_scanResultItems[i].Value, scanConstraint.UserInput, scanConstraint))
                passedMemorySegments.Add(_scanResultItems[i]);
        }

        selectedProcess.Dispose();
        //MainWindow.OpenProcessSelector();
    }
}
