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
    private INativeApi NativeApi { get; set; } = default!;

    private DotNetObjectReference<SelectProcess>? _dotNetHelper;

    protected override async Task OnInitializedAsync()
    {
        _processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .ToList();

        await base.OnInitializedAsync();
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            var module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/SelectProcess.razor.js");
            await module.InvokeVoidAsync("ready", JsonSerializer.Serialize(_processes.Select((x, i) => new { Name = x.DisplayString, x.IconBase64Source, x.Process.Id })), _dotNetHelper);
            await module.DisposeAsync();
        }
    }

    [JSInvokable]
    public void SetSelectedProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);
        EngineSession.SelectedProcess = new ProcessAdapter(process)
        {
            ProcessHandle = NativeApi.OpenProcess(processId)
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dotNetHelper?.Dispose();
    }
}
