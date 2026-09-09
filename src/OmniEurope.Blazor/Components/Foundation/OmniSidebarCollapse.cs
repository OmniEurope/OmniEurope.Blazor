namespace OmniEurope.Blazor.Components;

/// <summary>
/// What is left of a sidebar once it is closed.
/// </summary>
public enum OmniSidebarCollapse
{
    /// <summary>Nothing: the sidebar leaves the page entirely.</summary>
    Hidden,

    /// <summary>
    /// A rail of icons. Labels and unfolded groups go, the icons stay, and a group opens its own
    /// icons underneath when its icon is clicked.
    /// </summary>
    Icons
}
