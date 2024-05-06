using Microsoft.AspNetCore.Components;

namespace CelSerEngine.WpfBlazor.Components;

public class ContextMenuItem
{
    public string Text { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string KeyboardShortcut { get; set; } = string.Empty;
    internal EventCallback OnClick { get; set; }
}
