using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.IO;
using static CelSerEngine.Core.Native.Enums;

namespace CelSerEngine.WpfBlazor.Components;

internal class SearchSubmitModel
{
    [RequiredIf(nameof(SelectedScanCompareType), ScanCompareType.ValueBetween, negate: true, ErrorMessage = "Provide a value to scan for")]
    public string SearchValue { get; set; } = string.Empty;

    [RequiredIf(nameof(SelectedScanCompareType), ScanCompareType.ValueBetween, ErrorMessage = "Provide the start value of the range")]
    public string FromValue { get; set; } = string.Empty;

    [RequiredIf(nameof(SelectedScanCompareType), ScanCompareType.ValueBetween, ErrorMessage = "Provide the end value of the range")]
    public string ToValue { get; set; } = string.Empty;

    public ScanDataType SelectedScanDataType { get; set; } = ScanDataType.Integer;
    public ScanCompareType SelectedScanCompareType { get; set; } = ScanCompareType.ExactValue;

    [IsIntPtr(MaxValuePropertyName = nameof(StopAddress))]
    public string StartAddress { get; set; } = IntPtr.Zero.ToString("X");

    [IsIntPtr(MinValuePropertyName = nameof(StartAddress))]
    public string StopAddress { get; set; } = IntPtr.MaxValue.ToString("X");
    public MemoryScanFilterOptions Writable { get; set; } = MemoryScanFilterOptions.Yes;
    public MemoryScanFilterOptions Executable { get; set; } = MemoryScanFilterOptions.Dont_Care;
    public MemoryScanFilterOptions CopyOnWrite { get; set; } = MemoryScanFilterOptions.No;
    public MemoryType[] MemoryTypes { get; set; } = [MemoryType.Image, MemoryType.Private];
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
    private IMemoryScanService MemoryScanService { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private ThemeManager ThemeManager { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private MainWindow MainWindow { get; set; } = default!;

    private ScanResultItemsGrid ScanResultItemsGridRef { get; set; } = default!;
    private TrackedItemsGrid TrackedItemsGridRef { get; set; } = default!;
    private SearchSubmitModel SearchSubmitModel { get; set; } = new();
    private IList<ModuleInfo> Modules { get; set; } = [];
    private float ProgressBarValue { get; set; }
    private bool IsFirstScan { get; set; } = true;
    private bool IsScanning => ScanCancellationTokenSource != null;
    private CancellationTokenSource? ScanCancellationTokenSource { get; set; }

    private IJSObjectReference? _module;
    private readonly IProgress<float> _progressBarUpdater;

    public Index()
    {
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            if (newValue - ProgressBarValue >= 1)
            {
                ProgressBarValue = newValue;
                StateHasChanged();
            }
        });
    }

    protected override void OnInitialized()
    {
        EngineSession.OnChange += UpdateModules;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Index.razor.js");
            await _module.InvokeVoidAsync("initIndex");

            if (EngineSession.SelectedProcess != null)
            {
                UpdateModules();
            }
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
        await ScanResultItemsGridRef.ClearScanResultItemsAsync();
        var userInput = SearchSubmitModel.SearchValue;
        
        if (SearchSubmitModel.SelectedScanCompareType == ScanCompareType.ValueBetween)
        {
            userInput = $"{SearchSubmitModel.FromValue}-{SearchSubmitModel.ToValue}";
        }

        var scanConstraint = new ScanConstraint(SearchSubmitModel.SelectedScanCompareType, SearchSubmitModel.SelectedScanDataType, userInput)
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
        await ScanResultItemsGridRef.AddScanResultItemsAsync(results.Select(x => new ScanResultItem(x)));
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
        await ScanResultItemsGridRef.ShowScanningOverlayAsync();
        var userInput = SearchSubmitModel.SearchValue;

        if (SearchSubmitModel.SelectedScanCompareType == ScanCompareType.ValueBetween)
        {
            userInput = $"{SearchSubmitModel.FromValue}-{SearchSubmitModel.ToValue}";
        }

        var scanConstraint = new ScanConstraint(SearchSubmitModel.SelectedScanCompareType, SearchSubmitModel.SelectedScanDataType, userInput);
        var result = await MemoryScanService.FilterMemorySegmentsByScanConstraintAsync(
            ScanResultItemsGridRef.GetScanResultItems().Cast<IMemorySegment>().ToList(),
            scanConstraint,
            EngineSession.SelectedProcessHandle,
            _progressBarUpdater,
            token);
        _progressBarUpdater.Report(100);
        await ScanResultItemsGridRef.ClearScanResultItemsAsync(false);
        await ScanResultItemsGridRef.AddScanResultItemsAsync(result.Select(x => new ScanResultItem(x)));
        ProgressBarValue = 0;
        ScanCancellationTokenSource = null;
    }

    private async Task NewScan()
    {
        await ScanResultItemsGridRef.ResetScanResultItemsAsync();
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

    private async void UpdateModules()
    {
        Modules = NativeApi.GetProcessModules(EngineSession.SelectedProcessHandle);
        var moduleNames = Modules.Select(x => Path.GetFileName(x.Name)).Prepend("All").ToList();
        await _module!.InvokeVoidAsync("updateModules", moduleNames);
        StateHasChanged();
    }

    private void OnSelectedModuleChanged(ChangeEventArgs e)
    {
        var selectedModuleName = e.Value as string;

        if (selectedModuleName == "All")
        {
            SearchSubmitModel.StartAddress = IntPtr.Zero.ToString("X");
            SearchSubmitModel.StopAddress = IntPtr.MaxValue.ToString("X");

            return;
        }

        var selectedModuleInfo = Modules.First(x => Path.GetFileName(x.Name) == selectedModuleName);
        SearchSubmitModel.StartAddress = selectedModuleInfo.BaseAddress.ToString("X");
        SearchSubmitModel.StopAddress = (selectedModuleInfo.BaseAddress + selectedModuleInfo.Size).ToString("X");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        EngineSession.OnChange -= UpdateModules;

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
