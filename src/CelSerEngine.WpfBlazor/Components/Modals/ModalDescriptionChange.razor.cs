using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CelSerEngine.WpfBlazor.Components.Modals;

public partial class ModalDescriptionChange : ComponentBase
{
    [CascadingParameter]
    protected Modal ModalContainer { get; set; } = default!;

    [Parameter]
    public string Description { get; set; } = default!;

    [Parameter]
    public EventCallback<string> DescriptionChanged1 { get; set; } = default!;

    private EditContext EditContext { get; set; } = default!; // this is needed! without it the form is only submitted when double clicking submit button
                                                              // (this can be removed once we have a form model)

    protected override void OnInitialized()
    {
        EditContext = new(Description);
    }

    private async Task OnSubmitAsync()
    {
        await ModalContainer.HideModalAsync();

        if (DescriptionChanged1.HasDelegate)
        {
            await DescriptionChanged1.InvokeAsync(Description);
        }
    }
}
