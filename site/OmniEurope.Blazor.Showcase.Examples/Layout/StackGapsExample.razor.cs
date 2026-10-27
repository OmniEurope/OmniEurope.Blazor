using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The six values of the spacing scale, each applied to a row of three blocks.</summary>
public partial class StackGapsExample
{
    private static readonly OmniSpacing[] Gaps =
    [
        OmniSpacing.None,
        OmniSpacing.XSmall,
        OmniSpacing.Small,
        OmniSpacing.Medium,
        OmniSpacing.Large,
        OmniSpacing.ExtraLarge
    ];
}
