namespace OmniEurope.Blazor.Components;

public partial class OmniSteps
{
    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    [Parameter]
    public Func<int, Task<bool>>? CanNavigate { get; set; }

    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("StepsLabel")
        : Label;

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
