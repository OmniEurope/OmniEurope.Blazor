using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The vertical alignments of a row, each applied to three columns of unequal heights.</summary>
public partial class RowAlignmentsExample
{
    private static readonly OmniAlignment[] Alignments =
    [
        OmniAlignment.Start,
        OmniAlignment.Center,
        OmniAlignment.End,
        OmniAlignment.Stretch
    ];
}
