namespace OmniEurope.Blazor.Components;

public partial class OmniRadioButtonListItem<TValue>
{
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public bool Selected { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<TValue?> SelectedValueChanged { get; set; }

    private Task SelectAsync() => Disabled ? Task.CompletedTask : SelectedValueChanged.InvokeAsync(Value);
}
