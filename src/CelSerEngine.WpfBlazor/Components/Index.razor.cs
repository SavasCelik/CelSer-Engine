using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CelSerEngine.WpfBlazor.Components;

public class SearchSubmitModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Provide a value to scan for")]
    public string SearchValue { get; set; } = string.Empty;
    public ScanDataType SelectedScanDataType { get; set; } = ScanDataType.Integer;
    public ScanCompareType SelectedScanCompareType { get; set; } = ScanCompareType.ExactValue;
}

public partial class Index : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private MainWindow MainWindow { get; set; } = default!;

    private VirtualizedAgGrid<ScanResultItem> _virtualizedAgGridRef = default!;
    private TrackedItemsGrid _trackedItemsGridRef = default!;
    private List<ScanResultItem> _scanResultItems { get; set; } = [];
    private List<TrackedItem> _trackedItems { get; set; } = [];
    private SearchSubmitModel _searchSubmitModel { get; set; } = new();
    private float _progressBarValue { get; set; }
    private bool _isFirstScan { get; set; } = true;
    private readonly Timer _scanResultsUpdater;
    private readonly IProgress<float> _progressBarUpdater;
    private IJSObjectReference? _module;

    public Index()
    {
        _scanResultsUpdater = new Timer((e) => UpdateVisibleScanResults(), null, Timeout.Infinite, 0);
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            if (newValue - _progressBarValue >= 10)
            {
                _progressBarValue = newValue;
                StateHasChanged();
            }
        });
    }

    public async Task OnScanResultItemDoubleClicked(ScanResultItem scanResultItem)
    {
        _trackedItems.Add(new TrackedItem(scanResultItem));
        await _trackedItemsGridRef.RefreshDataAsync();
    }

    protected override void OnInitialized()
    {
        EngineSession.OnChange += StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Index.razor.js");
        }
    }

    private async Task SearchScanInvalidSubmit()
    {
        await _module!.InvokeVoidAsync("focusFirstInvalidInput");
    }

    private async Task FirstScan(EditContext formContext)
    {
        if (!formContext.Validate())
        {
            await SearchScanInvalidSubmit();
            return;
        }

        _scanResultItems.Clear();
        await _virtualizedAgGridRef.ApplyDataAsync();
        await _virtualizedAgGridRef.ShowScanningOverlay();
        var results = await Task.Run(() =>
        {
            var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(EngineSession.SelectedProcessHandle);
            var scanConstraint = new ScanConstraint(_searchSubmitModel.SelectedScanCompareType, _searchSubmitModel.SelectedScanDataType, _searchSubmitModel.SearchValue);
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);

            return comparer.GetMatchingMemorySegments(virtualMemoryRegions, _progressBarUpdater);
        });

        _progressBarValue = 100;
        StateHasChanged();
         _scanResultItems.AddRange(results.Select(x => new ScanResultItem(x)));
        await _virtualizedAgGridRef.ApplyDataAsync();
        _scanResultsUpdater.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        _progressBarValue = 0;
        _isFirstScan = false;
    }

    private void OpenSelectProcess()
    {
        MainWindow.OpenProcessSelector();
    }

    private async Task NextScan(EditContext formContext)
    {
        if (!formContext.Validate())
        {
            await SearchScanInvalidSubmit();
            return;
        }

        await _virtualizedAgGridRef.ShowScanningOverlay();
        var scanConstraint = new ScanConstraint(_searchSubmitModel.SelectedScanCompareType, _searchSubmitModel.SelectedScanDataType, _searchSubmitModel.SearchValue);
        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, _scanResultItems);
        var passedMemorySegments = new List<ScanResultItem>();

        for (var i = 0; i < _scanResultItems.Count; i++)
        {
            if (ValueComparer.MeetsTheScanConstraint(_scanResultItems[i].Value, scanConstraint.UserInput, scanConstraint))
            {
                _scanResultItems[i].PreviousValue = _scanResultItems[i].Value;
                passedMemorySegments.Add(_scanResultItems[i]);
            }
        }

        _scanResultItems.Clear();
        _scanResultItems.AddRange(passedMemorySegments);
        await _virtualizedAgGridRef.ApplyDataAsync();
    }

    private async Task NewScan()
    {
        _scanResultsUpdater.Change(Timeout.Infinite, 0);
        _scanResultItems.Clear();
        await _virtualizedAgGridRef.ResetGrid();
        _isFirstScan = true;
        await _module!.InvokeVoidAsync("focusSearchValueInput");
    }

    private async void UpdateVisibleScanResults()
    {
        var visibleItems = _virtualizedAgGridRef.GetVisibleItems().ToList();

        if (visibleItems.Count == 0)
            return;

        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, visibleItems);

        if (!_virtualizedAgGridRef.IsDisposed)
            await _virtualizedAgGridRef.ApplyDataAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        EngineSession.OnChange -= StateHasChanged;
        await _scanResultsUpdater.DisposeAsync();

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
