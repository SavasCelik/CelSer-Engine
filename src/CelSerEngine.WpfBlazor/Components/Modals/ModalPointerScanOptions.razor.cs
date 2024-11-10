using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CelSerEngine.WpfBlazor.Components.Modals;

public partial class ModalPointerScanOptions : ComponentBase
{
    [CascadingParameter]
    protected Modal ModalContainer { get; set; } = default!;

    [Inject]
    private MainWindow MainWindow { get; set; } = default!;

    [Parameter]
    public string ScanAddress { get; set; } = default!;

    //[Parameter]
    //public EventCallback<PointerScanOptions> OnSubmit { get; set; }

    private PointerScanOptions PointerScanOptions { get; } = new();

    protected override void OnInitialized()
    {
        //ValueChangeSubmitModel = new(Value);
        PointerScanOptions.ScanAddress = ScanAddress;
    }

    private async Task OnSubmitAsync(EditContext formContext)
    {
        if (!formContext.Validate())
        {
            return;
        }

        await ModalContainer.HideModalAsync();
        MainWindow.OpenPointerScanner();
        //await OnSubmit.InvokeAsync(PointerScanOptions);
    }
}

public class PointerScanOptions
{
    public string ScanAddress { get; set; } = string.Empty;
    public string Offset { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
