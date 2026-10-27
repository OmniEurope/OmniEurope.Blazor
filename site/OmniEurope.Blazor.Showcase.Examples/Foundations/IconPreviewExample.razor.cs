using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>One icon of the set, at a size and with an optional accessible label, and how a screen reader meets it.</summary>
public partial class IconPreviewExample
{
    /// <summary>The icon shown.</summary>
    [Parameter]
    public OmniIconName Name { get; set; } = OmniIconName.Star;

    /// <summary>The size of the icon.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Large;

    /// <summary>The accessible label; null or blank makes the icon decorative.</summary>
    [Parameter]
    public string? Label { get; set; } = "Favourite";

    private string AriaState => string.IsNullOrWhiteSpace(Label)
        ? "Decorative: aria-hidden, no role."
        : $"Announced as an image named \"{Label}\".";
}
