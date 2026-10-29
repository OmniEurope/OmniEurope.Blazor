namespace OmniEurope.Blazor.Components;

/// <summary>
/// A vertical navigation menu of <see cref="OmniPanelMenuItem"/>, groups folding their entries, the
/// entry of the current route marked; down to its icons on the rail of an <see cref="OmniSidebar"/>.
/// </summary>
public partial class OmniPanelMenu
{
    /// <summary>Accessible name of the navigation landmark. Null, the default, is the localized "Navigation".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Narrow the items down to their icons. The text stays in the markup so a screen reader still
    /// announces the destination, and a title attribute is not a substitute for it.
    /// </summary>
    [Parameter]
    public OmniPanelMenuDisplay Display { get; set; } = OmniPanelMenuDisplay.IconAndText;

    /// <summary>
    /// Shows a search field above the entries: typing narrows the menu to the entries whose text contains
    /// it, ignoring case and accents, with the groups holding a match unfolded for as long as the text is
    /// there. The field is not shown while the menu is down to its icons (the rail of a closed sidebar
    /// included).
    /// </summary>
    [Parameter]
    public bool ShowFilter { get; set; }

    /// <summary>
    /// The text the entries are narrowed by, ignoring case and accents. An entry whose text contains it is
    /// shown, with the groups above it; a group whose own text contains it is shown with all its entries;
    /// the others are hidden, and the groups are folded back as they were once the text is empty. It also
    /// narrows a menu without <see cref="ShowFilter"/>, for a host that brings its own field; it is ignored
    /// while the menu is down to its icons.
    /// </summary>
    [Parameter]
    public string? Filter { get; set; }

    /// <summary>Raised with the new text when the reader types in the field of <see cref="ShowFilter"/>.</summary>
    [Parameter]
    public EventCallback<string?> FilterChanged { get; set; }

    /// <summary>Placeholder and accessible name of the field; the localized "filter the menu" when empty.</summary>
    [Parameter]
    public string? FilterPlaceholder { get; set; }

    /// <summary>
    /// The sidebar this menu is nested in, when it is nested in one. A sidebar that keeps a rail of
    /// icons once closed decides the display style on its own: leaving that to the consumer means
    /// two places holding the same state, and one of them going stale.
    /// </summary>
    [CascadingParameter]
    private OmniSidebarState? Sidebar { get; set; }

    private OmniPanelMenuDisplay EffectiveDisplay => Sidebar?.IconsOnly == true
        ? OmniPanelMenuDisplay.Icon
        : Display;

    // Kept across renders: the first-level entries register in it once, and a context rebuilt on each
    // render would hand them a fresh, empty one.
    private readonly OmniPanelMenuSiblings _siblings = new();

    // The text last received as the parameter, so a new one from the host replaces what was typed and
    // an unchanged one (a host that does not bind it) leaves it alone.
    private string? _receivedFilterText;
    private string? _filterText;
    private OmniPanelMenuFilter _filter = OmniPanelMenuFilter.None;

    private OmniPanelMenuContext OwnContext => new(EffectiveDisplay)
    {
        ExpandSidebar = Sidebar?.Expand,
        CanNavigate = CanNavigate,
        Siblings = _siblings,
        Filter = _filter,
        ItemsChanged = () => _ = InvokeAsync(StateHasChanged)
    };

    private string EffectiveLabel => LocalizeOr(Label, "PanelMenuLabel");

    // Down to its icons a menu has no room for the field, and hiding entries the reader cannot see the
    // reason for would read as a broken menu.
    private bool ShowsFilterField => ShowFilter && EffectiveDisplay != OmniPanelMenuDisplay.Icon;

    private bool IsFiltering => _filter.Active && EffectiveDisplay != OmniPanelMenuDisplay.Icon;

    private bool HasNoMatch => IsFiltering && !_siblings.AnyMatchesFilter(_filter);

    private string EffectiveFilterPlaceholder => LocalizeOr(FilterPlaceholder, "PanelMenuFilter");

    /// <summary>
    /// Asked, with the address, before any entry of the menu navigates: the entry navigates only when
    /// it answers true, which lets the host keep a page with unsaved changes or handle the choice
    /// itself. Null, the default, lets the links navigate as plain links.
    /// </summary>
    [Parameter]
    public Func<string, Task<bool>>? CanNavigate { get; set; }

    /// <summary>The entries of the menu, <see cref="OmniPanelMenuItem"/> in their order, groups nesting their own.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }

    /// <summary>Takes <see cref="Filter"/> when the host changed it since the last time.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Filter != _receivedFilterText)
        {
            _receivedFilterText = Filter;
            ApplyFilterText(Filter);
        }
    }

    private async Task SetFilterTextAsync(string? text)
    {
        ApplyFilterText(text);
        await FilterChanged.InvokeAsync(text);
    }

    // A new filter object only when the effective text changed: the entries compare it by reference to
    // know they have to drop what they folded or unfolded under the previous one.
    private void ApplyFilterText(string? text)
    {
        _filterText = text;
        var next = new OmniPanelMenuFilter(text);
        if (next.Text != _filter.Text)
        {
            _filter = next;
        }
    }
}
