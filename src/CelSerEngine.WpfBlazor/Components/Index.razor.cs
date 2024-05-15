using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using static CelSerEngine.Core.Native.Enums;

namespace CelSerEngine.WpfBlazor.Components;

internal class SearchSubmitModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Provide a value to scan for")]
    public string SearchValue { get; set; } = string.Empty;
    public ScanDataType SelectedScanDataType { get; set; } = ScanDataType.Integer;
    public ScanCompareType SelectedScanCompareType { get; set; } = ScanCompareType.ExactValue;
    [IsIntPtr(MaxValuePropertyName = nameof(StopAddress))]
    public string StartAddress { get; set; } = IntPtr.Zero.ToString("X");
    [IsIntPtr(MinValuePropertyName = nameof(StartAddress))]
    public string StopAddress { get; set; } = IntPtr.MaxValue.ToString("X");
    public MemoryScanFilterOptions Writable { get; set; } = MemoryScanFilterOptions.Yes;
    public MemoryScanFilterOptions Executable { get; set; } = MemoryScanFilterOptions.Dont_Care;
    public MemoryScanFilterOptions CopyOnWrite { get; set; } = MemoryScanFilterOptions.No;
    public MemoryType[] MemoryTypes { get; set; } = { MemoryType.Image, MemoryType.Private };
}

internal enum MemoryScanFilterOptions
{
    Yes,
    No,
    [Display(Name = "Don't Care")]
    Dont_Care
}

internal enum MemoryType
{
    Image,
    Private,
    Mapped
}

public partial class Index : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private IMemoryScanService MemoryScanService { get; set; } = default!;

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
    private bool IsScanning => ScanCancellationTokenSource != null;
    private ICollection<ContextMenuItem> ContextMenuItems { get; set; }
    private CancellationTokenSource? ScanCancellationTokenSource { get; set; }

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

        ScanCancellationTokenSource = new CancellationTokenSource();
        var token = ScanCancellationTokenSource.Token;
        ScanResultItems.Clear();
        await VirtualizedAgGridRef.ApplyDataAsync();
        await VirtualizedAgGridRef.ShowScanningOverlay();
        var scanConstraint = new ScanConstraint(SearchSubmitModel.SelectedScanCompareType, SearchSubmitModel.SelectedScanDataType, SearchSubmitModel.SearchValue)
        {
            StartAddress = IntPtr.Parse(SearchSubmitModel.StartAddress, System.Globalization.NumberStyles.HexNumber),
            StopAddress = IntPtr.Parse(SearchSubmitModel.StopAddress, System.Globalization.NumberStyles.HexNumber)
        };

        foreach (var memoryType in SearchSubmitModel.MemoryTypes)
        {
            switch (memoryType)
            {
                case MemoryType.Private:
                    scanConstraint.AllowedMemoryTypes.Add(MEMORY_TYPE.MEM_PRIVATE);
                    break;
                case MemoryType.Image:
                    scanConstraint.AllowedMemoryTypes.Add(MEMORY_TYPE.MEM_IMAGE);
                    break;
                case MemoryType.Mapped:
                    scanConstraint.AllowedMemoryTypes.Add(MEMORY_TYPE.MEM_MAPPED);
                    break;
            }
        }

        if (SearchSubmitModel.Writable == MemoryScanFilterOptions.Yes)
        {
            scanConstraint.IncludedProtections |= MemoryProtections.Writable;
        }
        if (SearchSubmitModel.Executable == MemoryScanFilterOptions.Yes)
        {
            scanConstraint.IncludedProtections |= MemoryProtections.Executable;
        }
        if (SearchSubmitModel.CopyOnWrite == MemoryScanFilterOptions.Yes)
        {
            scanConstraint.IncludedProtections |= MemoryProtections.CopyOnWrite;
        }

        if (SearchSubmitModel.Writable == MemoryScanFilterOptions.No)
        {
            scanConstraint.ExcludedProtections |= MemoryProtections.Writable;
        }
        if (SearchSubmitModel.Executable == MemoryScanFilterOptions.No)
        {
            scanConstraint.ExcludedProtections |= MemoryProtections.Executable;
        }
        if (SearchSubmitModel.CopyOnWrite == MemoryScanFilterOptions.No)
        {
            scanConstraint.ExcludedProtections |= MemoryProtections.CopyOnWrite;
        }

        var results = await MemoryScanService.ScanProcessMemoryAsync(scanConstraint, EngineSession.SelectedProcessHandle, _progressBarUpdater, token);
        _progressBarUpdater.Report(100);
        ScanResultItems.AddRange(results.Select(x => new ScanResultItem(x)));
        await VirtualizedAgGridRef.ApplyDataAsync();
        StartScanResultValueUpdater();
        ProgressBarValue = 0;
        ScanCancellationTokenSource = null;
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

        ScanCancellationTokenSource = new CancellationTokenSource();
        var token = ScanCancellationTokenSource.Token;
        StopScanResultValueUpdater();
        await VirtualizedAgGridRef.ShowScanningOverlay();
        var scanConstraint = new ScanConstraint(SearchSubmitModel.SelectedScanCompareType, SearchSubmitModel.SelectedScanDataType, SearchSubmitModel.SearchValue);
        var result = await MemoryScanService.FilterMemorySegmentsByScanConstraintAsync(
            ScanResultItems.Cast<IMemorySegment>().ToList(),
            scanConstraint,
            EngineSession.SelectedProcessHandle,
            _progressBarUpdater,
            token);
        _progressBarUpdater.Report(100);

        if (result.Count > 0)
        {
            StartScanResultValueUpdater();
        }

        ScanResultItems.Clear();
        ScanResultItems.AddRange(result.Select(x => new ScanResultItem(x)));
        await VirtualizedAgGridRef.ApplyDataAsync();
        ProgressBarValue = 0;
        ScanCancellationTokenSource = null;
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
        if (ScanCancellationTokenSource != null)
        {
            await ScanCancellationTokenSource.CancelAsync();
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
