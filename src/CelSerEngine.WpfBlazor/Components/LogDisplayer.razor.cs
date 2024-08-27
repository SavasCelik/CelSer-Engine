using CelSerEngine.WpfBlazor.Loggers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CelSerEngine.WpfBlazor.Components;
public partial class LogDisplayer : ComponentBase, IAsyncDisposable
{
    [Inject] 
    public BlazorLogManager LogManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private ICollection<string> LogsContent { get; } = [];

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/LogDisplayer.razor.js");
        }
    }

    protected override void OnInitialized()
    {
        // Subscribe to the log events
        LogManager.OnLogReceived += HandleLogReceived;
    }

    private async void HandleLogReceived(string logMessage)
    {
        LogsContent.Add(logMessage);
        StateHasChanged();

        if (_module != null)
        {
            await _module.InvokeVoidAsync("scrollToBottom");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        LogManager.OnLogReceived -= HandleLogReceived;

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
