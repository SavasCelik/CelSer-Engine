using CelSerEngine.Core.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq.Expressions;

namespace CelSerEngine.WpfBlazor.Components.Selects;
public partial class SelectEnum<TItem> : ComponentBase
    where TItem : Enum
{

    [Parameter]
    public string Id { get; set; } = default!;
    /// <summary>
    /// Gets or sets the fixed item source.
    /// </summary>
    [Parameter]
    public ICollection<TItem> Items { get; set; } = default!;
    [Parameter]
    public TItem? Value { get; set; } = default!;
    [Parameter]
    public EventCallback<TItem> ValueChanged { get; set; }
    [Parameter] 
    public Expression<Func<TItem>>? ValueExpression { get; set; }

    public ICollection<TItem> CurrentItems { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current value of the input.
    /// </summary>
    protected TItem? CurrentValue
    {
        get => Value;
        set
        {
            var hasChanged = !EqualityComparer<TItem>.Default.Equals(value, Value);
            if (hasChanged)
            {

                // If we don't do this, then when the user edits from A to B, we'd:
                // - Do a render that changes back to A
                // - Then send the updated value to the parent, which sends the B back to this component
                // - Do another render that changes it to B again
                // The unnecessary reversion from B to A can cause selection to be lost while typing
                // A better solution would be somehow forcing the parent component's render to occur first,
                // but that would involve a complex change in the renderer to keep the render queue sorted
                // by component depth or similar.
                Value = value;

                _ = ValueChanged.InvokeAsync(Value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] 
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private bool _hasInitializedParameters;
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Selects/SelectEnum.razor.js");
            await _module.InvokeVoidAsync("initSelectEnum", Id, CurrentItems.Select(x => new { Id = x, Name = x.GetDisplayName() }));
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (CurrentItems?.Count != Items.Count)
        {
            CurrentItems = Items;
            if (_module != null)
            {
                var valueBeforeUpdate = CurrentValue;
                await _module.InvokeVoidAsync("updateSelect", CurrentItems.Select(x => new { Id = x, Name = x.GetDisplayName() }));
                CurrentValue = valueBeforeUpdate;
                await _module.InvokeVoidAsync("setSelection", CurrentValue);
            }
        }

        if (!_hasInitializedParameters)
        {
            if (ValueExpression == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                    $"parameter. Normally this is provided automatically when using 'bind-Value'.");
            }

            _hasInitializedParameters = true;
        }

        await base.SetParametersAsync(ParameterView.Empty);
    }
}
