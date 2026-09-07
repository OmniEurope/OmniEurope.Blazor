namespace OmniEurope.Blazor.Components;

public partial class OmniSidebarToggle
{
    [Parameter, EditorRequired]
    public string Controls { get; set; } = string.Empty;

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? Localize("SidebarToggleLabel")
        : AriaLabel;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Task ToggleAsync() => OpenChanged.InvokeAsync(!Open);
}
