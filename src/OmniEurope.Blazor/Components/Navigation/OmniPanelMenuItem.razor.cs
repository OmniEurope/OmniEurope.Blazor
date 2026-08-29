namespace OmniEurope.Blazor.Components;

public partial class OmniPanelMenuItem
{
    private readonly string _childrenId = $"omni-panel-menu-children-{Guid.NewGuid():N}";

    /// <summary>
    /// Null while the item has not been toggled by hand, which is what lets the active route decide
    /// the state. A hand toggle pins it until the route moves back inside this group.
    /// </summary>
    private bool? _open;

    private bool _wasActive;
    private OmniPanelMenuGroupContext? _ownContext;

    /// <summary>Group this item is nested in, if any, so it can report being the current page.</summary>
    [CascadingParameter]
    private OmniPanelMenuGroupContext? ParentGroup { get; set; }

    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Rendered before the text. A slot rather than a name, so the consumer keeps its own icon set.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public bool Current { get; set; }

    /// <summary>
    /// Initial state of a group. The active route still opens the group that contains it, and a
    /// hand toggle still wins over both.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public Func<string, Task<bool>>? CanNavigate { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool IsCurrent => Current || IsActive;

    private bool IsOpen => _open ?? (Expanded || IsWithin);

    /// <summary>
    /// A group reveals itself when the current page is one of its own entries, or when the route
    /// sits strictly below its address. Landing on the group's own page is not enough on its own,
    /// which is what keeps a section landing page from unfolding the section.
    /// </summary>
    private bool IsWithin => (_ownContext?.HasActiveChild ?? false) || Match == RouteMatch.Descendant;

    /// <summary>
    /// Context handed down to the nested items. Created on first use rather than in the field
    /// initializer, because it closes over a state change this instance must already own.
    /// </summary>
    private OmniPanelMenuGroupContext OwnContext => _ownContext ??= new OmniPanelMenuGroupContext(
        () => _ = InvokeAsync(StateHasChanged));

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

    private bool IsActive => Match is not (null or RouteMatch.None);

    private bool IsExactMatch => Match == RouteMatch.Exact;

    private RouteMatch? Match
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

    // Only a leaf reports: a nested group already unfolds through its own children, and counting it
    // as well would unfold its parent for a page nobody is on.
    private void ReportToParent() => ParentGroup?.Report(this, ChildContent is null && IsActive);

    private enum RouteMatch
    {
        None,
        Exact,
        Descendant
    }

    private void Toggle() => _open = !IsOpen;

    private async Task HandleNavigateAsync()
    {
        var href = SafeHref;
        if (CanNavigate is not null && href is not null && await CanNavigate(href))
        {
            Navigation.NavigateTo(href);
        }
    }

    public void Dispose()
    {
        ParentGroup?.Remove(this);
        Navigation.LocationChanged -= HandleLocationChanged;
    }

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
