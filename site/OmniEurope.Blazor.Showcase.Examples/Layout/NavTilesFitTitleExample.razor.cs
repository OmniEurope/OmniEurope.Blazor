using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Six narrow navigation tiles whose titles fit one line when FitTitle is on.</summary>
public partial class NavTilesFitTitleExample
{
    /// <summary>Whether each title shrinks to stay on one line instead of wrapping.</summary>
    [Parameter]
    public bool FitTitle { get; set; } = true;

    /// <summary>Where the icon and the title sit across each tile; null keeps the tile's default.</summary>
    [Parameter]
    public OmniAlignment? Alignment { get; set; }
}
