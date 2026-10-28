using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The surfaces of the mockup together: disc cards, a floating account menu, tabs, a side menu, a link and a tooltip.</summary>
public partial class SurfacesExample
{
    private static readonly (string Key, string Text)[] MenuEntries =
    [
        ("dashboard", "Dashboard"),
        ("servers", "Servers"),
        ("projects", "Projects"),
        ("administration", "Administration")
    ];

    /// <summary>Whether the three disc cards open the example; a page that already shows them elsewhere turns them off.</summary>
    [Parameter]
    public bool WithCards { get; set; } = true;

    private string? Tab { get; set; } = "overview";

    private string MenuCurrent { get; set; } = MenuEntries[0].Key;

    private string? Last { get; set; }

    /// <summary>The menu's guard: the address names the entry after its <c>#</c>; the example shows it in place and never navigates.</summary>
    private Task<bool> SelectAsync(string href)
    {
        MenuCurrent = href[(href.IndexOf('#', StringComparison.Ordinal) + 1)..];
        StateHasChanged();
        return Task.FromResult(false);
    }
}
