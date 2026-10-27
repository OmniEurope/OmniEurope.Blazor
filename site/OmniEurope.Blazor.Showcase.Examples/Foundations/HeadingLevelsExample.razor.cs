using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Every heading level, with the tag it renders.</summary>
public partial class HeadingLevelsExample
{
    private static readonly OmniHeadingLevel[] Levels =
    [
        OmniHeadingLevel.H1, OmniHeadingLevel.H2, OmniHeadingLevel.H3,
        OmniHeadingLevel.H4, OmniHeadingLevel.H5, OmniHeadingLevel.H6
    ];
}
