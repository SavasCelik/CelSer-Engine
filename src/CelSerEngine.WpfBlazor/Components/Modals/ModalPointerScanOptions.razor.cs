using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

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
    [IsIntPtr]
    public string ScanAddress { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Make sure the max. offset is not negative")]
    public int MaxOffset { get; set; } = 0x1000;
    [Range(1, 32)]
    public int MaxLevel { get; set; } = 4;
}
