using CelSerEngine.Core.Native;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;

public partial class SelectProcess : ComponentBase, IDisposable
{
    private List<ProcessAdapter> _processes = new();

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private ThemeManager ThemeManager { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!; 
    
    [Inject]
    private MainWindow MainWindow { get; set; } = default!;

    private DotNetObjectReference<SelectProcess>? _dotNetHelper;
    private IJSObjectReference? _module;

    protected override async Task OnInitializedAsync()
    {
        _processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .ToList();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/SelectProcess.razor.js");
            await _module.InvokeVoidAsync("ready", JsonSerializer.Serialize(_processes.Select((x, i) => new { Name = x.DisplayString, x.IconBase64Source, x.Process.Id })), _dotNetHelper);
        }
    }

    private async Task RefreshProcessList()
    {
        await _module!.InvokeVoidAsync("showLoadingOverlay");
        await Task.Run(() =>
        {
            _processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .ToList();
        });
        await _module!.InvokeVoidAsync("updateProcessList", JsonSerializer.Serialize(_processes.Select((x, i) => new { Name = x.DisplayString, x.IconBase64Source, x.Process.Id })));
    }

    [JSInvokable]
    public void SetSelectedProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);
        EngineSession.SelectedProcess = new ProcessAdapter(process)
        {
            ProcessHandle = NativeApi.OpenProcess(processId)
        };

        MainWindow.CloseProcessSelector();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // using IAsyncDisposable causes the closing method in BlazorWebViewWindow.xaml.cs to throw an exception
        _dotNetHelper?.Dispose();

        if (_module != null)
        {
            _module.DisposeAsync();
        }
    }
}
