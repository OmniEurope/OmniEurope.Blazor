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

    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The slices, in the order they are drawn clockwise from the top.</summary>
    [Parameter] public IReadOnlyList<OmniChartSlice> Data { get; set; } = Array.Empty<OmniChartSlice>();

    /// <summary>The name of the series, which the data table of the chart shows above its slices.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Draws the slices as a ring (a donut) around an empty centre instead of a full disc. Off by default.
    /// </summary>
    [Parameter] public bool Donut { get; set; }

    private string CssClass => Donut ? "omni-chart__donut" : "omni-chart__pie";

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
    }

    private string SlicePath(int index) => OmniChartGeometry.Arc(_angles[index], _angles[index + 1], 42, Donut);

    /// <summary>Removes the series from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterPie(this);
        GC.SuppressFinalize(this);
    }
}
