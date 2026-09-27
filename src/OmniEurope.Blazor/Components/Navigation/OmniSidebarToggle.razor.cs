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
    /// The glyph shown while the sidebar is open, when no content replaces the default one. Left unset,
    /// the toggle keeps the menu glyph while there is room for the sidebar beside the content, and turns
    /// into a close cross only under the phone threshold (39.99rem), where the open menu covers the page
    /// and has to say how to dismiss it. Set it to force one glyph at every width.
    /// </summary>
    [Parameter]
    public OmniIconName? OpenIcon { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Task ToggleAsync() => OpenChanged.InvokeAsync(!Open);
}
