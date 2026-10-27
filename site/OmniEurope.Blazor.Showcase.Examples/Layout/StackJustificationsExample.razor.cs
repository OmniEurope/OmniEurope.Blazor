using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The distributions of a horizontal stack, each applied to three blocks.</summary>
public partial class StackJustificationsExample
{
    private static readonly OmniJustification[] Justifications =
    [
        OmniJustification.Start,
        OmniJustification.Center,
        OmniJustification.End,
        OmniJustification.Between
    ];
}
