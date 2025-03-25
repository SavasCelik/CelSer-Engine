using CelSerEngine.Core.Models;
using CelSerEngine.WpfBlazor.Components.Modals;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace CelSerEngine.WpfBlazor.Components.PointerScanner;

public partial class ModalPointerScanOptions : ComponentBase
{
    [CascadingParameter]
    protected Modal ModalContainer { get; set; } = default!;

    [Parameter]
    public string SearchedAddress { get; set; } = default!;

    [Parameter]
    public EventCallback<PointerScanOptions> OnPointerScanOptionsSubmit { get; set; } = default!;

    //[Parameter]
    //public EventCallback<PointerScanOptionsSubmitModel> OnSubmit { get; set; }

    private PointerScanOptionsSubmitModel PointerScanOptionsSubmitModel { get; } = new();

    protected override void OnInitialized()
    {
        //ValueChangeSubmitModel = new(Value);
        PointerScanOptionsSubmitModel.SearchedAddress = SearchedAddress;
    }

    private async Task OnSubmitAsync(EditContext formContext)
    {
        if (!formContext.Validate())
            return;

        await ModalContainer.HideModalAsync();
        var pointerScanOptions = new PointerScanOptions
        {
            MaxOffset = PointerScanOptionsSubmitModel.MaxOffset,
            MaxLevel = PointerScanOptionsSubmitModel.MaxLevel,
            SearchedAddress = new IntPtr(long.Parse(PointerScanOptionsSubmitModel.SearchedAddress, NumberStyles.HexNumber))
        };
        await OnPointerScanOptionsSubmit.InvokeAsync(pointerScanOptions);
    }
}

public class PointerScanOptionsSubmitModel
{
    [IsIntPtr]
    public string SearchedAddress { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Make sure the max. offset is not negative")]
    public int MaxOffset { get; set; } = 0x1000;
    [Range(1, 32)]
    public int MaxLevel { get; set; } = 4;
}
