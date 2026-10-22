using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A chart drawn in SVG from its parts: series (<see cref="OmniLineSeries"/>, <see cref="OmniAreaSeries"/>,
/// <see cref="OmniColumnSeries"/>, <see cref="OmniPieSeries"/>), axes, grid lines, legend and titles, all
/// laid out on one shared plot.
/// </summary>
/// <remarks>
/// For assistive technology the drawing is one image named by <see cref="Label"/> and described by
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
    private ElementReference _figure;
    private IJSObjectReference? _module;
    private string? _appliedMaxHeight;
    private bool _disposed;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The accessible name of the chart, and the caption of the data table it writes.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

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
    /// HTML parts drawn under the drawing, before a legend below it, that share the chart's layout: an
    /// <see cref="OmniRangeNavigator"/>, which then limits the chart to the categories it selects. Null (the
    /// default) draws nothing there.
    /// </summary>
    [Parameter] public RenderFragment? FooterContent { get; set; }

    /// <summary>
    /// Width over height of the drawing. A wider value lets a line, area or column chart fill a wide,
    /// low card: the plot stretches, text keeps its size and a pie stays centred. Values under 1 are
    /// treated as 1. Left unset, the chart chooses: 2 for a time series (more than 12 categories on
    /// a vertical chart), 1 (square) otherwise; any value set, 1 included, is kept as given.
    /// </summary>
    [Parameter] public double AspectRatio { get; set; } = 1;

    /// <summary>
    /// One hover text per category instead of one per point: hovering anywhere in a category's band
    /// of the plot shows the category, then each series' title and value there, one line per series
    /// in their order, the values written by the <see cref="OmniValueAxis.FormatValue"/> of the
    /// chart's value axis (the current culture without one). The bands lie over the series, so the
    /// per-point hover texts give way to them; they are hidden from assistive technology, which reads
    /// the data table. Off by default: each point, column or marker keeps its own hover text.
    /// </summary>
    [Parameter] public bool SharedTooltip { get; set; }

    /// <summary>
    /// The narrowest the chart is drawn: in a narrower card (a phone) it keeps this width and scrolls
    /// sideways in its card, its legends and its <see cref="FooterContent"/> (a range navigator) with it,
    /// instead of shrinking until its text cannot be read. <see cref="OmniChartMinWidth.Auto"/> chooses
    /// it from the width over height of the drawing, and a host sets a length of its own through the
    /// <c>--omni-chart-min-width</c> property of its stylesheet; <see cref="OmniChartMinWidth.None"/>,
    /// the default, lets it shrink to any size.
    /// </summary>
    [Parameter] public OmniChartMinWidth MinWidth { get; set; } = OmniChartMinWidth.None;

    /// <summary>
    /// The tallest the drawing is shown, as a CSS length (<c>20rem</c>, <c>480px</c>, <c>60vh</c>), or
    /// <c>none</c> for no ceiling. Null, the default, keeps the ceiling of the stylesheet:
    /// <c>min(32rem, 70vh)</c>, which a host changes for every chart through the
    /// <c>--omni-chart-max-height</c> property of its stylesheet. Capped, the drawing keeps its
    /// proportions: it is fitted whole in the middle of the width of its card, its text scaled with it;
    /// the legends and the <see cref="FooterContent"/> keep the whole width. The value is written by the
    /// interop script as a CSS custom property (never as a style attribute), so a chart rendered without
    /// interactivity keeps the stylesheet ceiling.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    // The class of the minimum width; Auto takes about 12.5rem per unit of the drawing's width over height.
    private string? MinWidthClass
    {
        get
        {
            if (MinWidth == OmniChartMinWidth.None)
            {
                return null;
            }

            return (_context.ViewWidth / 100) switch
            {
                <= 1.2 => "omni-chart--min-small",
                <= 1.8 => "omni-chart--min-medium",
                <= 2.5 => "omni-chart--min-large",
                <= 3.5 => "omni-chart--min-xlarge",
                _ => "omni-chart--min-widest"
            };
        }
    }

    private bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    /// <summary>The bands of the shared tooltip, after the parts of the chart; nothing when it is off.</summary>
    private RenderFragment? SharedTooltipLayer => SharedTooltip
        ? builder =>
        {
            builder.OpenComponent<OmniChartSharedTooltip>(0);
            builder.AddComponentParameter(1, nameof(OmniChartSharedTooltip.Context), _context);
            builder.AddComponentParameter(2, nameof(OmniChartSharedTooltip.SeriesName), (Func<OmniChartContext.ChartSeriesView, int, string>)SeriesName);
            builder.CloseComponent();
        }
        : null;

    /// <summary>An entry of an HTML legend, dimmed while the series it names is hidden.</summary>
    private string LegendItemClass(OmniChartContext.LegendEntry entry) =>
        _context.IsEntryHidden(entry) ? "omni-chart__legend-item omni-chart__legend-item--hidden" : "omni-chart__legend-item";

    // Bound to the button of an entry that can toggle, which names a series by its colour.
    private void ToggleSeries(OmniChartContext.LegendEntry entry) => _context.ToggleSeries(entry.SeriesColor!.Value);

    private string SvgClass => _context.IsWide ? "omni-chart__svg omni-chart__svg--wide" : "omni-chart__svg";

    private string ViewBox => FormattableString.Invariant($"{_context.ViewLeft:0.###} 0 {_context.ViewWidth:0.###} 100");

    private string TitleId => $"{Id ?? _generatedId}-title";

    private string DescriptionId => $"{Id ?? _generatedId}-description";

    /// <summary>The id of the clip of the parts, only while a range is shown.</summary>
    private string? ClipId => _context.Range is null ? null : $"{Id ?? _generatedId}-clip";

    private string? ClipReference => ClipId is null ? null : $"url(#{ClipId})";

    private string ClipLeft => OmniChartGeometry.Number(_context.ViewLeft - 10);

    private string ClipWidth => OmniChartGeometry.Number(_context.ViewWidth + 20);

    /// <summary>Notes whether <see cref="AspectRatio"/> is among the parameters given, then sets them.</summary>
    /// <param name="parameters">The parameters supplied by the parent.</param>
    /// <returns>The task of the base implementation.</returns>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _aspectRatioSet = parameters.TryGetValue<double>(nameof(AspectRatio), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <summary>
    /// Hands the aspect ratio to the layout: the value given, when it was set or differs from 1, else
    /// none, so the layout chooses from the series.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="MaxHeight"/> is neither a CSS length nor <c>none</c>.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (MaxHeight is not null && !MaxHeightValue().IsMatch(MaxHeight))
        {
            throw new ArgumentException($"'{MaxHeight}' is not a CSS length {nameof(OmniChart)} accepts for {nameof(MaxHeight)} (a number followed by px, rem, em or vh, or none).", nameof(MaxHeight));
        }

        // A preset may set the ratio without the markup naming it: a value other than the default counts as set.
        _context.SetAspectRatio(_aspectRatioSet || AspectRatio != 1 ? AspectRatio : null);
    }

    /// <summary>Starts listening to the layout of the parts, and redraws the chart whenever it changes.</summary>
    protected override void OnInitialized() => _context.Changed += HandleProjectionChanged;

    private void HandleProjectionChanged() => _ = InvokeAsync(StateHasChanged);

    /// <summary>The column header of a series in the data table: its title, else "Series 2".</summary>
    private string SeriesName(OmniChartContext.ChartSeriesView series, int index) =>
        string.IsNullOrWhiteSpace(series.Title) ? Localize("ChartSeriesFallback", index + 1) : series.Title;

    /// <summary>The row header of a category: its axis label, else the X value of the first series that has the row.</summary>
    private string RowName(IReadOnlyList<OmniChartContext.ChartSeriesView> series, int row) =>
        _context.CategoryLabel(row)
        ?? series.Where(item => row < item.Data.Count).Select(item => OmniChartGeometry.Display(item.Data[row].X)).First();

    /// <summary>
    /// Hands a <see cref="MaxHeight"/> that changed to the interop script, loaded only once a value has
    /// to be written; a chart that never sets it loads no script. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>The task of the write.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || string.Equals(MaxHeight, _appliedMaxHeight, StringComparison.Ordinal))
        {
            return;
        }

        var maxHeight = MaxHeight;
        try
        {
            // A chart disposed while the import was pending releases the module on arrival.
            _module ??= await OmniScriptModule.ImportAsync(JavaScript, OmniModules.Interop, () => _disposed);
            if (_module is not null && !_disposed)
            {
                await _module.InvokeVoidAsync("applyChartMaxHeight", _figure, maxHeight);
                _appliedMaxHeight = maxHeight;
            }
        }
        catch (JSDisconnectedException)
        {
            // The page is gone; nothing left to size.
        }
    }

    /// <summary>Stops listening to the layout of the parts.</summary>
    public void Dispose()
    {
        _context.Changed -= HandleProjectionChanged;
        GC.SuppressFinalize(this);
    }

    /// <summary>Stops listening to the layout of the parts and releases the interop script, if it was loaded.</summary>
    /// <returns>The task of the release.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Dispose();
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The page is gone with the script.
        }
    }

    [GeneratedRegex("^(\\d+(\\.\\d+)?(px|rem|em|vh)|none)$", RegexOptions.CultureInvariant)]
    private static partial Regex MaxHeightValue();
}
