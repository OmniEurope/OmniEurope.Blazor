namespace OmniEurope.Blazor.Components;

/// <summary>
/// Shares of a whole drawn as the slices of a disc, or of a ring with <see cref="Donut"/>, each in the
/// next colour of the palette. Slices of zero or less are left out.
/// </summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniPieSeries
{
    private IReadOnlyList<OmniChartSlice> _slices = Array.Empty<OmniChartSlice>();
    private double[] _angles = [0];
    private double _radius = PieLabelLayout.FullRadius;
    private IReadOnlyList<PieLabelLayout.Label> _labels = [];

    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The slices, in the order they are drawn clockwise from the top.</summary>
    [Parameter] public IReadOnlyList<OmniChartSlice> Data { get; set; } = Array.Empty<OmniChartSlice>();

    /// <summary>The name of the series, which the data table of the chart shows above its slices.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Draws the slices as a ring (a donut) around an empty centre instead of a full disc. Off by default.
    /// </summary>
    [Parameter] public bool Donut { get; set; }

    /// <summary>
    /// Writes beside each slice, outside the disc, its name, with its value or its share of the whole if
    /// asked, joined to the middle of the slice by a leader line. The labels of each side are stacked so
    /// that none overlaps the next, and the disc shrinks to leave them room (down to a floor, below which
    /// a label too long is shortened, its hover text whole). <see cref="OmniPieLabels.None"/> by default.
    /// </summary>
    [Parameter] public OmniPieLabels OutsideLabels { get; set; }

    /// <summary>Writes a value for <see cref="OmniPieLabels.NameAndValue"/>; by default the number in the current culture.</summary>
    [Parameter] public Func<double, string>? FormatValue { get; set; }

    private string CssClass => Donut ? "omni-chart__donut" : "omni-chart__pie";

    /// <summary>Keeps the slices above zero, computes their angles in proportion to their sum, and registers them with the chart.</summary>
    protected override void OnParametersSet()
    {
        _slices = [.. Data.Where(slice => slice.Value > 0)];
        var total = Math.Max(double.Epsilon, _slices.Sum(slice => slice.Value));
        _angles = new double[_slices.Count + 1];
        for (var index = 0; index < _slices.Count; index++)
        {
            _angles[index + 1] = _angles[index] + (_slices[index].Value / total * 359.999);
        }

        ChartContext?.RegisterPie(this, Title, _slices);
        LayOutLabels(total);
    }

    private void LayOutLabels(double total)
    {
        if (OutsideLabels == OmniPieLabels.None || _slices.Count == 0)
        {
            _radius = PieLabelLayout.FullRadius;
            _labels = [];
            return;
        }

        var texts = _slices.Select(slice => OutsideLabels switch
        {
            OmniPieLabels.NameAndValue => $"{slice.Label} {FormatValue?.Invoke(slice.Value) ?? OmniChartGeometry.Display(slice.Value)}",
            OmniPieLabels.NameAndPercent => $"{slice.Label} {(slice.Value / total).ToString("P0", CultureInfo.CurrentCulture)}",
            _ => slice.Label
        }).ToArray();
        var halfWidth = 50 + (ChartContext?.Spread ?? 0);
        _radius = PieLabelLayout.Radius(texts, halfWidth);
        var middles = Enumerable.Range(0, _slices.Count).Select(index => (_angles[index] + _angles[index + 1]) / 2).ToArray();
        _labels = PieLabelLayout.Place(texts, middles, _radius, halfWidth);
    }

    private string SlicePath(int index) => OmniChartGeometry.Arc(_angles[index], _angles[index + 1], _radius, Donut);

    /// <summary>Removes the series from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterPie(this);
        GC.SuppressFinalize(this);
    }
}
