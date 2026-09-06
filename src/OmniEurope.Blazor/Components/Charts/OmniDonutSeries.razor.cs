namespace OmniEurope.Blazor.Components;

public partial class OmniDonutSeries
{
    private int _index;
    private double _angle;
    private double _total;
    [Parameter] public IReadOnlyList<OmniChartSlice> Data { get; set; } = Array.Empty<OmniChartSlice>();
    [Parameter] public string? Title { get; set; }
    private IEnumerable<OmniChartSlice> Slices
    {
        get
        {
            _index = 0;
            _angle = 0;
            _total = Math.Max(double.Epsilon, Data.Where(slice => slice.Value > 0).Sum(slice => slice.Value));
            return Data.Where(slice => slice.Value > 0);
        }
    }
    private string SlicePath(OmniChartSlice slice)
    {
        var start = _angle;
        _angle += slice.Value / _total * 359.999;
        return OmniChartGeometry.Arc(start, _angle, 42, true);
    }
}
