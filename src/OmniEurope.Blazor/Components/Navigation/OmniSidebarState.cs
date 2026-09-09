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
}
