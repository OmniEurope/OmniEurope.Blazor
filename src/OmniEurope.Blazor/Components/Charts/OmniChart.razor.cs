namespace OmniEurope.Blazor.Components;

/// <summary>
/// A chart drawn in SVG from its parts: series (<see cref="OmniLineSeries"/>, <see cref="OmniAreaSeries"/>,
/// <see cref="OmniColumnSeries"/>, <see cref="OmniPieSeries"/>), axes, grid lines, legend and titles, all
/// laid out on one shared plot.
/// </summary>
/// <remarks>
/// For assistive technology the drawing is one image named by <see cref="Title"/> and described by
/// <see cref="Description"/> when it is set. Its data is read from a table after it: the table of the
/// host in <see cref="DataTableContent"/>, shown to everyone, or else one the chart writes itself,
/// visually hidden, with a row per category and a column per series (one table per pie). Numbers there
/// and on the chart are written in the current culture.
/// </remarks>
public partial class OmniChart
{
    private readonly OmniChartContext _context = new();
    private readonly string _generatedId = $"omni-chart-{Guid.NewGuid():N}";
    private bool _aspectRatioSet;

    /// <summary>The accessible name of the chart, and the caption of the data table it writes.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>A sentence on what the chart shows, read after its name; none when null (the default).</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>The parts of the chart: series, axes, grid lines, legend, axis titles.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// A data table of the host, shown under the chart for everyone. Null (the default), the chart writes
    /// a visually hidden table of its series for screen readers instead.
    /// </summary>
    [Parameter] public RenderFragment? DataTableContent { get; set; }

    /// <summary>
    /// Width over height of the drawing. A wider value lets a line, area or column chart fill a wide,
    /// low card: the plot stretches, text keeps its size and a pie stays centred. Values under 1 are
    /// treated as 1. Left unset, the chart chooses: 2 for a time series (more than 12 categories on
    /// a vertical chart), 1 (square) otherwise; any value set, 1 included, is kept as given.
    /// </summary>
    [Parameter] public double AspectRatio { get; set; } = 1;

    private bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    private string SvgClass => _context.IsWide ? "omni-chart__svg omni-chart__svg--wide" : "omni-chart__svg";

    private string ViewBox => FormattableString.Invariant($"{_context.ViewLeft:0.###} 0 {_context.ViewWidth:0.###} 100");

    private string TitleId => $"{Id ?? _generatedId}-title";

    private string DescriptionId => $"{Id ?? _generatedId}-description";

    public override Task SetParametersAsync(ParameterView parameters)
    {
        _aspectRatioSet = parameters.TryGetValue<double>(nameof(AspectRatio), out _);
        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A preset may set the ratio without the markup naming it: a value other than the default counts as set.
        _context.SetAspectRatio(_aspectRatioSet || AspectRatio != 1 ? AspectRatio : null);
    }

    protected override void OnInitialized() => _context.Changed += HandleProjectionChanged;

    private void HandleProjectionChanged() => _ = InvokeAsync(StateHasChanged);

    /// <summary>The column header of a series in the data table: its title, else "Series 2".</summary>
    private string SeriesName(OmniChartContext.ChartSeriesView series, int index) =>
        string.IsNullOrWhiteSpace(series.Title) ? Localize("ChartSeriesFallback", index + 1) : series.Title;

    /// <summary>The row header of a category: its axis label, else the X value of the first series that has the row.</summary>
    private string RowName(IReadOnlyList<OmniChartContext.ChartSeriesView> series, int row) =>
        _context.CategoryLabel(row)
        ?? series.Where(item => row < item.Data.Count).Select(item => OmniChartGeometry.Display(item.Data[row].X)).First();

    /// <summary>Stops listening to the layout of the parts.</summary>
    public void Dispose()
    {
        _context.Changed -= HandleProjectionChanged;
        GC.SuppressFinalize(this);
    }
}
