namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a sidebar tells the navigation nested inside it. A panel menu cannot read the sidebar's
/// parameters, and asking the consumer to keep its own display style in step with the sidebar's
/// open state means two places to change and one to forget.
/// </summary>
/// <param name="Open">Whether the sidebar is currently open.</param>
/// <param name="Collapse">What the sidebar leaves behind once closed.</param>
public sealed record OmniSidebarState(bool Open, OmniSidebarCollapse Collapse)
{
    /// <summary>The nested navigation is down to its icons: closed, over a sidebar that keeps a rail.</summary>
    public bool IconsOnly => !Open && Collapse == OmniSidebarCollapse.Icons;

    /// <summary>
    /// Asks the sidebar to open, through its <c>OpenChanged</c>. A group clicked on the rail has no room
    /// to show its entries, so it opens the sidebar instead of unfolding in place. Null when nothing
    /// can open the sidebar.
    /// </summary>
    public Func<Task>? Expand { get; init; }
}
