using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The alignments of a horizontal stack, each applied to three blocks of unequal heights.</summary>
public partial class StackAlignmentsExample
{
    private static readonly OmniAlignment[] Alignments =
    [
        OmniAlignment.Start,
        OmniAlignment.Center,
        OmniAlignment.End,
        OmniAlignment.Stretch
    ];
}
