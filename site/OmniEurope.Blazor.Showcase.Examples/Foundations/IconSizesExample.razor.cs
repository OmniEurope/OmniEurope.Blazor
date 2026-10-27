using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>One icon at each of the three sizes.</summary>
public partial class IconSizesExample
{
    private static readonly OmniControlSize[] Sizes = Enum.GetValues<OmniControlSize>();

    /// <summary>The icon shown at every size.</summary>
    [Parameter]
    public OmniIconName Name { get; set; } = OmniIconName.Star;
}
