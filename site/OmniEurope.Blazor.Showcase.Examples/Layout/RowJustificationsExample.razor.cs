using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The distributions of a row, each applied to two columns that leave half of it free.</summary>
public partial class RowJustificationsExample
{
    private static readonly OmniJustification[] Justifications =
    [
        OmniJustification.Start,
        OmniJustification.Center,
        OmniJustification.End,
        OmniJustification.Between
    ];
}
