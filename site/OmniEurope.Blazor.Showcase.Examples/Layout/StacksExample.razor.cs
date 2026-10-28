using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A stack of buttons whose orientation, gap, alignment, distribution and overflow are parameters.</summary>
public partial class StacksExample
{
    /// <summary>The direction of the stack: this example starts as a row (horizontal), while OmniStack itself is a column (vertical) by default.</summary>
    [Parameter]
    public OmniOrientation Orientation { get; set; } = OmniOrientation.Horizontal;

    /// <summary>The space between two items, from the spacing scale.</summary>
    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    /// <summary>The position of the items across the axis.</summary>
    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Center;

    /// <summary>The position of the items along the axis.</summary>
    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;

    /// <summary>What a horizontal stack does when it runs out of room.</summary>
    [Parameter]
    public OmniStackOverflow Overflow { get; set; } = OmniStackOverflow.None;

    /// <summary>How many of the eight buttons the stack holds.</summary>
    [Parameter]
    public int ItemCount { get; set; } = 4;

    /// <summary>Whether the stack sits in a column of half the width, to try the overflow.</summary>
    [Parameter]
    public bool Narrow { get; set; }

    private string? Last { get; set; }
}
