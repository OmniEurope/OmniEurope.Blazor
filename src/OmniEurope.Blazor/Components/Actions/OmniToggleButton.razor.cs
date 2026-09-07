namespace OmniEurope.Blazor.Components;

public partial class OmniToggleButton
{
    [Parameter]
    public bool Value { get; set; }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Task ToggleAsync() => Disabled || Busy ? Task.CompletedTask : ValueChanged.InvokeAsync(!Value);
}
