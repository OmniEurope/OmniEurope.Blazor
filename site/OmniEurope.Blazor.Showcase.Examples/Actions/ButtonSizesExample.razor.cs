using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>Every button variant at every size, with a label or as an icon-only square.</summary>
public partial class ButtonSizesExample
{
    /// <summary>The three sizes, smallest first.</summary>
    private static readonly OmniControlSize[] Sizes = [OmniControlSize.Small, OmniControlSize.Medium, OmniControlSize.Large];

    /// <summary>Whether the buttons show their icon alone.</summary>
    [Parameter]
    public bool IconOnly { get; set; }

    private int ClickCount { get; set; }

    private void CountClick() => ClickCount++;
}
