namespace OmniEurope.Blazor.Components;

/// <summary>The numbered steps of a process, <see cref="OmniStepsItem"/>, the selected one showing its panel.</summary>
public partial class OmniSteps
{
    /// <summary>The index of the selected step, from zero, for <c>@bind-Value</c>.</summary>
    [Parameter]
    public int Value { get; set; }

    /// <summary>Raised with the index of the step the reader selects, once <see cref="CanNavigate"/> allows it.</summary>
    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    /// <summary>Asked before the reader moves to a step (its index); false keeps the current one. Null, the default, allows every move.</summary>
    [Parameter]
    public Func<int, Task<bool>>? CanNavigate { get; set; }

    /// <summary>Accessible name of the list of steps. Null, the default, is the localized "Steps".</summary>
    [Parameter]
    public string? Label { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "StepsLabel");

    /// <summary>The steps, <see cref="OmniStepsItem"/> in their order.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private OmniStepsContext Context => new() { Value = Value, SelectAsync = SelectAsync };

    private async Task SelectAsync(int index)
    {
        if (CanNavigate is null || await CanNavigate(index))
        {
            await ValueChanged.InvokeAsync(index);
        }
    }
}
