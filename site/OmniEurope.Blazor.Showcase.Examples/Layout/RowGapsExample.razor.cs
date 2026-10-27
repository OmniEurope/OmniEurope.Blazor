using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Two badges in a row, once per value of the spacing scale.</summary>
public partial class RowGapsExample
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
