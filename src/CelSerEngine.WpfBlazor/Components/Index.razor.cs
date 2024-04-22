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
        await _virtualizedAgGridRef.ShowScanningOverlay();
        var process = Process.GetProcessesByName("SmallGame").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var scanConstraint = new ScanConstraint(_selectedScanCompareType, _selectedScanDataType, _searchValue);
        NativeApi.UpdateAddresses(pHandle, _scanResultItems);
        var passedMemorySegments = new List<ScanResultItem>();

        for (var i = 0; i < _scanResultItems.Count; i++)
        {
            if (ValueComparer.MeetsTheScanConstraint(_scanResultItems[i].Value, scanConstraint.UserInput, scanConstraint))
            {
                _scanResultItems[i].PreviousValue = _scanResultItems[i].Value;
                passedMemorySegments.Add(_scanResultItems[i]);
        }
        }

        selectedProcess.Dispose();
        _scanResultItems.Clear();
        _scanResultItems.AddRange(passedMemorySegments);
        await _virtualizedAgGridRef.ApplyDataAsync();
    }
    }
}
