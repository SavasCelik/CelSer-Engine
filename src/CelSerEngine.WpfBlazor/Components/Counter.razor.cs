using Microsoft.AspNetCore.Components;

namespace CelSerEngine.WpfBlazor.Components;

public partial class Counter : ComponentBase
{
    [Inject]
    private MainWindow? MainWindow { get; set; }

    protected string Message { get; set; } = "Hellow";

    private void OpenSelectProcess()
    {
        MainWindow.OpenProcessSelector();
    }
}
