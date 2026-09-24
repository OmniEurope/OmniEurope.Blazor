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

    /// <summary>
    /// The glyph shown while the sidebar is open, when no content replaces the default one: a close
    /// cross, so an open menu says how to dismiss it. Pass <see cref="OmniIconName.Menu"/> to keep
    /// the same glyph in both states, as a pushing sidebar beside the content usually does.
    /// </summary>
    [Parameter]
    public OmniIconName OpenIcon { get; set; } = OmniIconName.Close;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Task ToggleAsync() => OpenChanged.InvokeAsync(!Open);
}
