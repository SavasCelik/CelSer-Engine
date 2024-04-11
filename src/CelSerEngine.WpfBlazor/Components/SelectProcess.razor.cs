using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;

public partial class SelectProcess : ComponentBase
{
    private List<ProcessAdapter> _processes = new();

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

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
            var module = await JS.InvokeAsync<IJSObjectReference>("import", "/js/selectProcess.js");
            await module.InvokeVoidAsync("ready", JsonSerializer.Serialize(_processes.Select((x, i) => new { Name = x.DisplayString, x.IconBase64Source }).ToList()));
        }
    }
}
