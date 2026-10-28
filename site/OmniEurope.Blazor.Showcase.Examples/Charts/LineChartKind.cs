namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>How the series of a line chart are drawn.</summary>
public enum LineChartKind
{
    /// <summary>One line per series.</summary>
    Line,

    /// <summary>One filled area per series.</summary>
    Area,

    /// <summary>Filled areas stacked on each other.</summary>
    StackedArea
}
