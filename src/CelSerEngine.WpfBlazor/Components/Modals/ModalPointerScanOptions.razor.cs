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
    //public EventCallback<PointerScanOptionsSubmitModel> OnSubmit { get; set; }

    private PointerScanOptionsSubmitModel PointerScanOptionsSubmitModel { get; } = new();

    protected override void OnInitialized()
    {
        //ValueChangeSubmitModel = new(Value);
        PointerScanOptionsSubmitModel.ScanAddress = ScanAddress;
    }

    private async Task OnSubmitAsync(EditContext formContext)
    {
        if (!formContext.Validate())
        {
            return;
        }

        await ModalContainer.HideModalAsync();
        MainWindow.OpenPointerScanner(PointerScanOptionsSubmitModel);
        //await OnSubmit.InvokeAsync(PointerScanOptionsSubmitModel);
    }
}

public class PointerScanOptionsSubmitModel
{
    public string ScanAddress { get; set; } = string.Empty;
    public string Offset { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
