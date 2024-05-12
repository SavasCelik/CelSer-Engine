using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.WpfBlazor.Components;

internal class SearchSubmitModel
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
    private ThemeManager ThemeManager { get; set; } = default!;

    [Inject]
    private MainWindow MainWindow { get; set; } = default!;

    private VirtualizedAgGrid<ScanResultItem> VirtualizedAgGridRef { get; set; } = default!;
    private TrackedItemsGrid TrackedItemsGridRef { get; set; } = default!;
    private List<ScanResultItem> ScanResultItems { get; set; } = [];
    private List<TrackedItem> TrackedItems { get; set; } = [];
    private SearchSubmitModel SearchSubmitModel { get; set; } = new();
    private float ProgressBarValue { get; set; }
    private bool IsFirstScan { get; set; } = true;
    private ICollection<ContextMenuItem> ContextMenuItems { get; set; }

    private CancellationTokenSource? _scanCancellationTokenSource;
    private IJSObjectReference? _module;
    private readonly Timer _scanResultsUpdater;
    private readonly IProgress<float> _progressBarUpdater;

    public Index()
    {
        _scanResultsUpdater = new Timer((e) => UpdateVisibleScanResults(), null, Timeout.Infinite, 0);
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            if (newValue - ProgressBarValue >= 1)
            {
                ProgressBarValue = newValue;
                StateHasChanged();
            }
        });

        ContextMenuItems =
        [
            new ContextMenuItem
            {
                Text = "Add selected addresses to the tracked addresses",
                OnClick = EventCallback.Factory.Create(this, ContextMenuItemClickedAsync)
            },
        ];
    }

    public async Task ContextMenuItemClickedAsync()
    {
        var selectedScanResultItems = ScanResultItems.Where(x => VirtualizedAgGridRef.SelectedItems.Contains(x.Address.ToString("X")));
        TrackedItems.AddRange(selectedScanResultItems.Select(x => new TrackedItem(x)));
        await TrackedItemsGridRef.RefreshDataAsync();
    }

    public async Task OnScanResultItemDoubleClicked(ScanResultItem scanResultItem)
    {
        TrackedItems.Add(new TrackedItem(scanResultItem));
        await TrackedItemsGridRef.RefreshDataAsync();
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

    private void ChangeTheme()
    {
        ThemeManager.ToggleTheme();
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

        _scanCancellationTokenSource = new CancellationTokenSource();
        var token = _scanCancellationTokenSource.Token;
        ScanResultItems.Clear();
        await VirtualizedAgGridRef.ApplyDataAsync();
        await VirtualizedAgGridRef.ShowScanningOverlay();
        var results = await Task.Run(() =>
        {
            var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(EngineSession.SelectedProcessHandle);
            var scanConstraint = new ScanConstraint(SearchSubmitModel.SelectedScanCompareType, SearchSubmitModel.SelectedScanDataType, SearchSubmitModel.SearchValue);
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);

            return comparer.GetMatchingMemorySegments(virtualMemoryRegions, _progressBarUpdater, token);
        }, token);

        ProgressBarValue = 100;
        StateHasChanged();
        ScanResultItems.AddRange(results.Select(x => new ScanResultItem(x)));
        await VirtualizedAgGridRef.ApplyDataAsync();
        StartScanResultValueUpdater();
        ProgressBarValue = 0;
        _scanCancellationTokenSource = null;
        IsFirstScan = false;
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

        _scanCancellationTokenSource = new CancellationTokenSource();
        var token = _scanCancellationTokenSource.Token;
        StopScanResultValueUpdater();
        await VirtualizedAgGridRef.ShowScanningOverlay();

        var result = await Task.Run(() => {
            var scanConstraint = new ScanConstraint(SearchSubmitModel.SelectedScanCompareType, SearchSubmitModel.SelectedScanDataType, SearchSubmitModel.SearchValue);
            NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, ScanResultItems, token);
            var passedMemorySegments = new List<ScanResultItem>();

            for (var i = 0; i < ScanResultItems.Count; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                if (ValueComparer.MeetsTheScanConstraint(ScanResultItems[i].Value, scanConstraint.UserInput, scanConstraint))
                {
                    ScanResultItems[i].PreviousValue = ScanResultItems[i].Value;
                    passedMemorySegments.Add(ScanResultItems[i]);
                }
            }

            return passedMemorySegments;
        }, token);

        if (result.Count > 0)
        {
            StartScanResultValueUpdater();
        }

        ScanResultItems.Clear();
        ScanResultItems.AddRange(result);
        await VirtualizedAgGridRef.ApplyDataAsync();
        _scanCancellationTokenSource = null;
    }

    private async Task NewScan()
    {
        StopScanResultValueUpdater();
        ScanResultItems.Clear();
        await VirtualizedAgGridRef.ResetGrid();
        IsFirstScan = true;
        await _module!.InvokeVoidAsync("focusSearchValueInput");
    }

    private async Task CancelScan()
    {
        if (_scanCancellationTokenSource != null)
        {
            await _scanCancellationTokenSource.CancelAsync();
        }
    }

    private void StartScanResultValueUpdater()
    {
        _scanResultsUpdater.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void StopScanResultValueUpdater()
    {
        _scanResultsUpdater.Change(Timeout.Infinite, 0);
    }

    private async void UpdateVisibleScanResults()
    {
        var visibleItems = VirtualizedAgGridRef.GetVisibleItems().ToList();

        if (visibleItems.Count == 0)
            return;

        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, visibleItems);

        if (!VirtualizedAgGridRef.IsDisposed)
            await VirtualizedAgGridRef.ApplyDataAsync();
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
