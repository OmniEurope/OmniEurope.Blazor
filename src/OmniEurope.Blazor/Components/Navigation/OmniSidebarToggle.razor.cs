namespace OmniEurope.Blazor.Components;

/// <summary>
/// The button that opens and closes an <see cref="OmniSidebar"/>, rendered with <c>aria-controls</c> and
/// <c>aria-expanded</c>. It is controlled: a click raises <see cref="OpenChanged"/> with the opposite of
/// <see cref="Open"/>. Its accessible name is <see cref="Label"/>, localized by default.
/// </summary>
public partial class OmniSidebarToggle
{
    /// <summary>The <c>id</c> of the sidebar the toggle controls, rendered as <c>aria-controls</c>. Required.</summary>
    [Parameter, EditorRequired]
    public string Controls { get; set; } = string.Empty;

    /// <summary>True when the controlled sidebar is open, rendered as <c>aria-expanded</c>. False by default.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised on every click with the opposite of <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Accessible name of the toggle. Null, the default, is the localized "Show or hide navigation".</summary>
    [Parameter]
    public string? Label { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "SidebarToggleLabel");

    /// <summary>
    /// The glyph shown while the sidebar is open, when no content replaces the default one. Left unset,
    /// the toggle keeps the menu glyph while there is room for the sidebar beside the content, and turns
    /// into a close cross only under the phone threshold (39.99rem), where the open menu covers the page
    /// and has to say how to dismiss it. Set it to force one glyph at every width.
    /// </summary>
    [Parameter]
    public OmniIconName? OpenIcon { get; set; }

    /// <summary>
    /// How the controlled sidebar opens, when the host knows it. Set, the open toggle follows the mode
    /// rather than the width: <see cref="OmniSidebarReveal.Push"/> keeps the menu glyph, since the sidebar
    /// sits beside the content, and <see cref="OmniSidebarReveal.Overlay"/> shows the close cross, since the
    /// sidebar covers the page. Pass the reveal the sidebar actually uses, including the overlay a host
    /// switches to on a phone. Null, the default, falls back to the width rule of <see cref="OpenIcon"/>;
    /// <see cref="OpenIcon"/> wins when both are set.
    /// </summary>
    [Parameter]
    public OmniSidebarReveal? Reveal { get; set; }

    /// <summary>
    /// Replaces the default glyphs, in both states. Null, the default, shows the menu glyph while closed
    /// and the glyph described on <see cref="OpenIcon"/> while open.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Task ToggleAsync() => OpenChanged.InvokeAsync(!Open);
}
