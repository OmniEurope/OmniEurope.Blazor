namespace OmniEurope.Blazor.Components;

/// <summary>
/// The axis of values, with its graduations: on the left of a vertical chart, along the bottom of a
/// horizontal one. Its bounds set the value domain of the chart, fixed or taken from the series.
/// </summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniValueAxis
{
    private OmniChartContext? _standalone;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The lowest value of the axis, unless <see cref="Automatic"/>.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>The highest value of the axis, unless <see cref="Automatic"/>; greater than <see cref="Minimum"/>.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    /// <summary>
    /// Takes the bounds from the series instead of <see cref="Minimum"/> and <see cref="Maximum"/>: from zero (or the
    /// lowest negative value) up to the highest value, both rounded outward to a step of 1, 2, 2.5 or 5 times a power
    /// of ten, so the graduations read as round numbers and no value is cut off. Off by default.
    /// </summary>
    [Parameter] public bool Automatic { get; set; }

    /// <summary>Number of intervals between graduations, 5 by default and at least 1; the axis writes one more value than this.</summary>
    [Parameter] public int TickCount { get; set; } = 5;

    /// <summary>Writes a graduation as text; by default the number in the current culture, at most three decimals.</summary>
    [Parameter] public Func<double, string>? FormatValue { get; set; }

    /// <summary>The chart's layout, or one of its own when the axis is drawn outside a chart.</summary>
    private OmniChartContext Context => ChartContext ?? (_standalone ??= new OmniChartContext());

    private IEnumerable<double> Ticks
    {
        get
        {
            var (minimum, maximum) = Automatic ? Context.ValueBounds : (Minimum, Maximum);
            return Enumerable.Range(0, TickCount + 1)
                .Select(index => minimum + ((maximum - minimum) * index / TickCount));
        }
    }

    /// <summary>
    /// Registers the axis with the chart (or with its own layout outside a chart): automatic, with its
    /// tick count, or with its fixed bounds, and its <see cref="FormatValue"/>, which the chart's
    /// shared tooltip writes its values with.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="TickCount"/> is zero or less, or fixed bounds where <see cref="Maximum"/> is not greater than <see cref="Minimum"/>.
    /// </exception>
    protected override void OnParametersSet()
    {
        if (TickCount <= 0) throw new ArgumentOutOfRangeException(nameof(TickCount), TickCount, "TickCount must be greater than zero.");
        Context.SetValueFormat(this, FormatValue);
        if (Automatic)
        {
            Context.RegisterAutomaticValueAxis(this, TickCount);
            return;
        }
        if (Maximum <= Minimum) throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum must be greater than Minimum.");
        Context.RegisterValueAxis(this, Minimum, Maximum);
    }

    private string Format(double value) => FormatValue?.Invoke(value) ?? OmniChartGeometry.Display(value);

    private static string N(double value) => OmniChartGeometry.Number(value);

    /// <summary>Removes the axis from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterValueAxis(this);
        GC.SuppressFinalize(this);
    }
}
