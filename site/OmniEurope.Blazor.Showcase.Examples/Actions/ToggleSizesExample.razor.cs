using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>The toggle button at its three sizes, with a label and as an icon-only square.</summary>
public partial class ToggleSizesExample
{
    private static readonly OmniControlSize[] Sizes = [OmniControlSize.Small, OmniControlSize.Medium, OmniControlSize.Large];

    private bool Pinned { get; set; }
}
