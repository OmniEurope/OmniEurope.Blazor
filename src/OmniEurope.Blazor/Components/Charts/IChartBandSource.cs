namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the <see cref="ChartBandLayout"/> reads of the chart it lays out: the registered series, the
/// shown category range, the plot rectangle and the value scale. Implemented by the chart context.
/// </summary>
internal interface IChartBandSource
{
    /// <summary>The series shown in the plot, in registration order.</summary>
    IEnumerable<OmniChartContext.SeriesRegistration> DrawnSeries { get; }

    /// <summary>The index of the first shown category.</summary>
    int FirstCategory { get; }

    /// <summary>The number of category bands across the plot.</summary>
    int CategoryCount { get; }

    /// <summary>The left edge of the plot rectangle.</summary>
    double PlotLeft { get; }

    /// <summary>The right edge of the plot rectangle.</summary>
    double PlotRight { get; }

    /// <summary>The top edge of the plot rectangle.</summary>
    double PlotTop { get; }

    /// <summary>The bottom edge of the plot rectangle.</summary>
    double PlotBottom { get; }

    /// <summary>The registration of the series drawn by <paramref name="owner"/>.</summary>
    OmniChartContext.SeriesRegistration GetSeries(object owner);

    /// <summary>Whether category <paramref name="index"/> is shown.</summary>
    bool InRange(int index);

    /// <summary>A point of the plot as an SVG coordinate pair.</summary>
    string Project(double x, double y);

    /// <summary>The vertical position of a value.</summary>
    double ValueToY(double value);

    /// <summary>The horizontal position of a value.</summary>
    double ValueToX(double value);
}
