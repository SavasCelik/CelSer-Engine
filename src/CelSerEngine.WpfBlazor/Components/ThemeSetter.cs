using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CelSerEngine.WpfBlazor.Components;

public partial class ThemeSetter : ComponentBase, IDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    public ThemeManager ThemeManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        ThemeManager.OnThemeChanged += SetTheme;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SetTheme();
        }
    }

    private void SetTheme()
    {
        JSRuntime.InvokeVoidAsync("setTheme", ThemeManager.IsDark ? "dark" : "light");
    }

    public void Dispose()
    {
        ThemeManager.OnThemeChanged -= SetTheme;
    }
}