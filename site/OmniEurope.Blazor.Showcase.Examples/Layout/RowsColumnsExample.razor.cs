using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A row of identical columns whose gap, alignment, distribution, wrapping and spans are parameters.</summary>
public partial class RowsColumnsExample
{
    /// <summary>The space between the columns; they deduct it from their width.</summary>
    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    /// <summary>The vertical position of columns of different heights.</summary>
    [Parameter]
    public OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    /// <summary>Where columns that do not fill the twelve units go.</summary>
    [Parameter]
    public OmniJustification Justify { get; set; } = OmniJustification.Start;

    /// <summary>Whether columns beyond twelve units go to the next line.</summary>
    [Parameter]
    public bool Wrap { get; set; } = true;

    /// <summary>How many columns the row holds.</summary>
    [Parameter]
    public int ColumnCount { get; set; } = 3;

    /// <summary>The width of every column, from 1 to 12 units, at every screen size.</summary>
    [Parameter]
    public int Span { get; set; } = 4;

    /// <summary>Whether the three breakpoint spans are set.</summary>
    [Parameter]
    public bool Responsive { get; set; }

    /// <summary>The width from 40rem (640 px) up.</summary>
    [Parameter]
    public int SmallSpan { get; set; } = 6;

    /// <summary>The width from 64rem (1024 px) up.</summary>
    [Parameter]
    public int MediumSpan { get; set; } = 4;

    /// <summary>The width from 80rem (1280 px) up.</summary>
    [Parameter]
    public int LargeSpan { get; set; } = 3;

    private string Caption(int number) => Responsive
        ? $"Column {number}, span {Span} (sm {SmallSpan}, md {MediumSpan}, lg {LargeSpan})"
        : $"Column {number}, span {Span}";
}
