using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>A breadcrumb trail whose depth, current page and unlinked level are parameters.</summary>
public partial class BreadcrumbExample
{
    /// <summary>The number of levels of the full path.</summary>
    public const int LevelCount = 4;

    /// <summary>The number of levels shown, from the first.</summary>
    [Parameter]
    public int Depth { get; set; } = LevelCount;

    /// <summary>Whether the last level shown is marked as the current page.</summary>
    [Parameter]
    public bool MarkCurrent { get; set; } = true;

    /// <summary>Whether the Navigation level has no address and is written as text.</summary>
    [Parameter]
    public bool UnlinkedStep { get; set; }

    /// <summary>The full path to the page. The trail shows its first levels only.</summary>
    private IReadOnlyList<Crumb> AllCrumbs =>
    [
        new("Home", "", false),
        new("Components", "components", false),
        new("Navigation", UnlinkedStep ? null : "components/tabs", false),
        new("Breadcrumbs and links", "components/breadcrumbs-links", false)
    ];

    private IReadOnlyList<Crumb> VisibleCrumbs
    {
        get
        {
            var crumbs = AllCrumbs.Take(Math.Clamp(Depth, 1, AllCrumbs.Count)).ToList();
            if (MarkCurrent)
            {
                crumbs[^1] = crumbs[^1] with { Current = true };
            }

            return crumbs;
        }
    }

    private sealed record Crumb(string Text, string? Href, bool Current);
}
