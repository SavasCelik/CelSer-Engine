using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.WpfBlazor.Components.Modals;

internal class ValueChangeSubmitModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Provide a value")]
    public string DesiredValue { get; set; }

    public ValueChangeSubmitModel(string value)
    {
        DesiredValue = value;
    }
}

public partial class ModalValueChange : ComponentBase
{
    [CascadingParameter]
    protected Modal ModalContainer { get; set; } = default!;

    [Parameter]
    public string Value { get; set; } = default!;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; } = default!;

    private ValueChangeSubmitModel ValueChangeSubmitModel { get; set; } = default!;

    protected override void OnInitialized()
    {
        ValueChangeSubmitModel = new(Value);
    }

    private async Task OnSubmitAsync(EditContext formContext)
    {
        if (!formContext.Validate())
        {
            return;
        }

        await ModalContainer.HideModalAsync();

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(ValueChangeSubmitModel.DesiredValue);
        }
    }
}
