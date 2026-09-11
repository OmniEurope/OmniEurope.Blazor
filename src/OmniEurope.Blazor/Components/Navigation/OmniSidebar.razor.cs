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

    /// <summary>
    /// What the application header shows over the sidebar's column, typically the toggle and the
    /// brand, rendered at the top of the panel while it floats open. Given this, an open floating
    /// panel rises to the top of the viewport instead of starting under the header: the veil then
    /// dims the rest of the header but not the part the menu belongs to, and since the copy sits
    /// where the original was, nothing moves as the panel opens. Without it, the panel starts under
    /// the header as before.
    /// </summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

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

    protected override void OnInitialized() => Navigation.LocationChanged += HandleLocationChanged;

    /// <summary>
    /// A floating sidebar covers the page it leads to, so choosing an entry closes it, veil
    /// included. A pushing sidebar stays: it shares the width and hides nothing.
    /// </summary>
    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        if (Open && Reveal == OmniSidebarReveal.Overlay)
        {
            _ = InvokeAsync(CloseAsync);
        }
    }

    public void Dispose() => Navigation.LocationChanged -= HandleLocationChanged;
}
