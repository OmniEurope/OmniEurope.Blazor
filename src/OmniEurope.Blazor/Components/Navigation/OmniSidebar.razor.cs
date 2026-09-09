namespace OmniEurope.Blazor.Components;

public partial class OmniSidebar
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Open { get; set; }

    /// <summary>
    /// Raised when the sidebar closes itself, which today means the veil was clicked. Without a
    /// handler the veil still renders and still swallows the click, so a floating sidebar that can
    /// be dismissed needs this bound.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public OmniSidebarPosition Position { get; set; }

    /// <summary>Whether opening pushes the content aside or floats over it.</summary>
    [Parameter]
    public OmniSidebarReveal Reveal { get; set; } = OmniSidebarReveal.Push;

    /// <summary>Whether closing removes the sidebar or leaves a rail of icons.</summary>
    [Parameter]
    public OmniSidebarCollapse Collapse { get; set; } = OmniSidebarCollapse.Hidden;

    /// <summary>
    /// Dims the content behind an open floating sidebar and closes it on click. Meaningless while
    /// the sidebar pushes, since nothing is covered then.
    /// </summary>
    [Parameter]
    public bool Backdrop { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public string CloseLabel { get; set; } = string.Empty;

    /// <summary>
    /// A closed sidebar still renders when it leaves a rail behind: the rail is what is left of it,
    /// not a separate control.
    /// </summary>
    private bool Rendered => Open || Collapse == OmniSidebarCollapse.Icons;

    private bool ShowBackdrop => Open && Backdrop && Reveal == OmniSidebarReveal.Overlay;

    private OmniSidebarState State => new(Open, Collapse);

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? Localize("SidebarLabel")
        : AriaLabel;

    private string EffectiveCloseLabel => string.IsNullOrWhiteSpace(CloseLabel)
        ? Localize("SidebarClose")
        : CloseLabel;

    private Task CloseAsync() => OpenChanged.InvokeAsync(false);
}
