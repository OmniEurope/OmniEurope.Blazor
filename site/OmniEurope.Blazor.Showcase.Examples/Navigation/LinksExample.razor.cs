using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>A list of links that open in the same tab or in a new one.</summary>
public partial class LinksExample
{
    private static readonly IReadOnlyList<SampleLink> Links =
    [
        new("Tabs page", "components/tabs"),
        new("Steps page", "components/steps"),
        new("Profile menu page", "components/profile-menu")
    ];

    /// <summary>Whether the links open in a new tab.</summary>
    [Parameter]
    public bool NewTab { get; set; }

    private sealed record SampleLink(string Text, string Href);
}
