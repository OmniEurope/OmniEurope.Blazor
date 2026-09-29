namespace OmniEurope.Blazor.Components;

/// <summary>
/// One entry of <see cref="OmniPanelMenu"/>: a link (<see cref="Href"/>), an action (<see cref="OnClick"/>),
/// a label, or a group that folds its child entries. The entry matching the current route is marked
/// current and the groups holding it unfold.
/// </summary>
public partial class OmniPanelMenuItem
{
    private readonly string _childrenId = $"omni-panel-menu-children-{Guid.NewGuid():N}";

    /// <summary>
    /// Null while the item has not been toggled by hand, which is what lets the active route decide
    /// the state. A hand toggle pins it until the route moves back inside this group.
    /// </summary>
    private bool? _open;

    private bool _wasActive;
    private bool? _lastExpanded;
    private OmniPanelMenuGroupContext? _ownContext;

    /// <summary>Group this item is nested in, if any, so it can report being the current page.</summary>
    [CascadingParameter]
    private OmniPanelMenuGroupContext? ParentGroup { get; set; }

    /// <summary>Menu this item belongs to, which is what knows whether items are down to their icon.</summary>
    [CascadingParameter]
    private OmniPanelMenuContext? Menu { get; set; }

    /// <summary>
    /// True while this item renders as an icon alone. A group then has no chevron to click, so its
    /// own icon takes over the unfolding.
    /// </summary>
    private bool IconsOnly => Menu?.IconsOnly ?? false;

    /// <summary>The text of the entry, also its accessible name when the menu is down to its icons.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Rendered before the text. A slot rather than a name, so the consumer keeps its own icon set.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Rendered after the text, at the end of the entry: a count or a state the entry carries (unread
    /// items, a running timer). Hidden with the text when the menu is down to its icons. Null renders
    /// nothing, as before.
    /// </summary>
    [Parameter]
    public RenderFragment? Badge { get; set; }

    /// <summary>The address the entry navigates to, checked by the package's URI policy. Null, the default, for an action, a label or a group without a page.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Marks the entry as the current page whatever the route says (<c>aria-current="page"</c>).</summary>
    [Parameter]
    public bool Current { get; set; }

    /// <summary>
    /// How <see cref="Href"/> is compared to the current route. Prefix by default, so a section
    /// entry stays lit inside its section; Exact for the landing page of an area, whose address is
    /// the prefix of every page below it and which would otherwise never go out.
    /// </summary>
    [Parameter]
    public OmniNavMatch Match { get; set; }

    /// <summary>
    /// Whether a group is unfolded, for <c>@bind-Expanded</c>. False by default: the active route still
    /// opens the group that contains it. A new value from the host wins over a hand toggle, and a hand
    /// toggle wins over the route until the route moves back inside the group.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>Raised with the new state when the reader unfolds or folds the group by hand.</summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    /// <summary>
    /// Makes an entry without <see cref="Href"/> and without children an action: a button of the menu's look
    /// (switching an account, copying a snippet) rather than an inert label. Ignored when the entry
    /// navigates or holds children.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>The child entries, which make this entry a group.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// One current entry per menu: a group whose address prefixes the route gives way when one of its
    /// own entries is the current page, or the menu lit the section and the page together (a server
    /// group and that server's overview). An explicit <see cref="Current"/> still wins.
    /// </summary>
    private bool IsCurrent => Current || (IsActive && !(_ownContext?.HasActiveChild ?? false));

    private bool IsOpen => _open ?? (Expanded || IsWithin);

    /// <summary>
    /// A group reveals itself when the current page is one of its own entries, or when the route
    /// sits strictly below its address. Landing on the group's own page is not enough on its own,
    /// which is what keeps a section landing page from unfolding the section.
    /// </summary>
    private bool IsWithin => (_ownContext?.HasActiveChild ?? false) || RouteState == RouteMatch.Descendant;

    /// <summary>
    /// Context handed down to the nested items. Created on first use rather than in the field
    /// initializer, because it closes over a state change this instance must already own.
    /// </summary>
    private OmniPanelMenuGroupContext OwnContext => _ownContext ??= new OmniPanelMenuGroupContext(() =>
    {
        // A group that has just learned it holds the current page has to pass that up in turn, or a
        // menu deeper than two levels leaves the outer group closed over the branch it should reveal.
        ReportToParent();
        _ = InvokeAsync(StateHasChanged);
    });

    private string ChildrenId => _childrenId;

    private string ToggleLabel => Localize("PanelMenuToggle", Text);

    private string LinkClass => Css("omni-panel-menu__link", IsCurrent ? "omni-panel-menu__link--current" : null);

    /// <summary>
    /// Same classes as <see cref="LinkClass"/> minus the consumer's own class, which the group
    /// already carries on its outer element and must not repeat on the inner control.
    /// </summary>
    private string GroupLinkClass => IsCurrent
        ? "omni-panel-menu__link omni-panel-menu__link--current"
        : "omni-panel-menu__link";

    // Exact narrows what counts as current, not what counts as being inside a group: a collapsed
    // group still unfolds on a descendant route, which is what IsWithin reads.
    private bool IsActive => Match switch
    {
        OmniNavMatch.None => false,
        OmniNavMatch.Exact => RouteState == RouteMatch.Exact,
        _ => RouteState is not (null or RouteMatch.None)
    };

    private RouteMatch? RouteState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Href)) return null;
            var currentUri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var targetUri = Navigation.ToAbsoluteUri(SafeHref!);
            if (targetUri.Scheme is not ("http" or "https") ||
                !string.Equals(currentUri.Scheme, targetUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(currentUri.Authority, targetUri.Authority, StringComparison.OrdinalIgnoreCase))
            {
                return RouteMatch.None;
            }

            var current = Uri.UnescapeDataString(currentUri.AbsolutePath).Trim('/');
            var target = Uri.UnescapeDataString(targetUri.AbsolutePath).Trim('/');
            if (target.Length == 0)
            {
                return current.Length == 0 ? RouteMatch.Exact : RouteMatch.None;
            }

            if (current.Equals(target, StringComparison.OrdinalIgnoreCase)) return RouteMatch.Exact;
            return current.StartsWith(target + '/', StringComparison.OrdinalIgnoreCase) ? RouteMatch.Descendant : RouteMatch.None;
        }
    }

    /// <summary>
    /// Reports to the parent group whether this entry holds the current page, and starts following the
    /// route so a group unfolds when the reader navigates into it.
    /// </summary>
    protected override void OnInitialized()
    {
        _wasActive = IsWithin;
        ReportToParent();
        Navigation.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        ReportToParent();

        // Entering the group releases a hand toggle, so navigating to a child always reveals it.
        // Leaving it does not, or collapsing a group would silently undo itself on the next click.
        var isWithin = IsWithin;
        if (isWithin && !_wasActive)
        {
            _open = null;
        }

        _wasActive = isWithin;
        _ = InvokeAsync(StateHasChanged);
    }

    // A leaf reports the route it matches; a group reports the page its own children hold, never its
    // own address, or a section landing page nobody is on would unfold the section above it.
    private void ReportToParent() =>
        ParentGroup?.Report(this, ChildContent is null ? IsActive : _ownContext?.HasActiveChild ?? false);

    private enum RouteMatch
    {
        None,
        Exact,
        Descendant
    }

    /// <summary>
    /// Applies a change of <see cref="Expanded"/> by the host as if the reader had toggled the group. The
    /// first value is only the initial state, which the current route may still override.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // A value the host changes is applied as if the reader had toggled the group; the first one
        // is only the initial state, which the route may still override.
        if (_lastExpanded is { } last && last != Expanded)
        {
            _open = Expanded;
        }

        _lastExpanded = Expanded;
    }

    private Task Toggle() => SetOpenAsync(!IsOpen);

    private async Task SetOpenAsync(bool open)
    {
        var changed = open != IsOpen;
        _open = open;
        if (changed)
        {
            await ExpandedChanged.InvokeAsync(open);
        }
    }

    /// <summary>
    /// On the rail a group has no room to show its entries, so its icon opens the sidebar on the
    /// group, unfolded. Without a sidebar that can be opened, it unfolds in place as before.
    /// </summary>
    private async Task HandleGroupClickAsync()
    {
        var expand = Menu?.ExpandSidebar;
        if (IconsOnly && expand is not null)
        {
            await SetOpenAsync(true);
            await expand();
            return;
        }

        await Toggle();
    }

    private async Task HandleNavigateAsync()
    {
        var href = SafeHref;
        var canNavigate = Menu?.CanNavigate;
        if (canNavigate is not null && href is not null && await canNavigate(href))
        {
            Navigation.NavigateTo(href);
        }
    }

    /// <summary>Stops following the route and leaves the parent group.</summary>
    public void Dispose()
    {
        ParentGroup?.Remove(this);
        Navigation.LocationChanged -= HandleLocationChanged;
    }

    private bool GuardsNavigation => Menu?.CanNavigate is not null;

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
