using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CelSerEngine.WpfBlazor.Components.Modals;

public partial class Modal : ComponentBase, IAsyncDisposable
{
    [Parameter] 
    public EventCallback OnModalReady { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;
    private Guid Id { get; set; } = Guid.NewGuid();
    private string Title { get; set; } = "Modal title";
    private RenderFragment? ContentComponentRenderFragment { get; set; }

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Modals/Modal.razor.js");
            await _module.InvokeVoidAsync("initModal", Id);

            if (OnModalReady.HasDelegate)
            {
                await OnModalReady.InvokeAsync();
            }
        }
    }

    public async Task ShowAsync<TComponent>(string title, IDictionary<string, object>? parameters = null) where TComponent : IComponent
    {
        Title = title;
        ContentComponentRenderFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent<TComponent>(0);

            if (parameters != null)
            {
                foreach (var entry in parameters)
                {
                    builder.AddComponentParameter(1, entry.Key, entry.Value);
                }
            }

            builder.CloseComponent();
        });
        StateHasChanged();
        await _module!.InvokeVoidAsync("showModal");
    }

    public async Task HideModalAsync()
    {
        await _module!.InvokeVoidAsync("hideModal");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
