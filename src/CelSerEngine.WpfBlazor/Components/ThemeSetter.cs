using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CelSerEngine.WpfBlazor.Components;

public partial class ThemeSetter : ComponentBase, IDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    public ThemeManager ThemeManager { get; set; } = default!;

    public bool IsDark { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        IsDark = await JSRuntime.InvokeAsync<bool>("isDarkTheme");
    }

    public async Task SetTheme(bool isDark)
    {
        await JSRuntime.InvokeVoidAsync("setTheme", isDark ? "dark" : "light");
    }

    public void Dispose()
    {

    }
}