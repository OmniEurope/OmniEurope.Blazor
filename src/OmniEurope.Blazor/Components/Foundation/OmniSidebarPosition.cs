namespace OmniEurope.Blazor.Components;

/// <summary>
/// The side of the page an <see cref="OmniSidebar"/> sits on, named by inline edge so it holds in a
/// right-to-left reading direction.
/// </summary>
public enum OmniSidebarPosition
{
    /// <summary>The side text begins on (left in a left-to-right page). The default.</summary>
    Start,

    /// <summary>The side text ends on (right in a left-to-right page).</summary>
    End
}
