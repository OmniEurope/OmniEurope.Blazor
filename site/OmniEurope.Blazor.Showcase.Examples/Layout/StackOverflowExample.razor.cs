using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The three overflow behaviours of a horizontal stack, each in a column too narrow for five buttons.</summary>
public partial class StackOverflowExample
{
    private static readonly OmniStackOverflow[] Overflows =
    [
        OmniStackOverflow.None,
        OmniStackOverflow.Scroll,
        OmniStackOverflow.Collapse
    ];
}
