namespace OmniEurope.Blazor.Components;

/// <summary>
/// A range navigator under an <see cref="OmniChart"/>: an overview of every category of the chart in
/// small columns, with a start handle and an end handle that choose the categories the chart above
/// shows. The chart redraws while a handle moves: its series, category axis, hover bands and data table
/// keep the categories of the range, and an automatic value axis fits them.
/// </summary>
/// <remarks>
/// Written in the <see cref="OmniChart.FooterContent"/> of its chart, never among the parts drawn in
/// it. Categories are counted by index, like the category axis and the data table: index <c>i</c> is
/// label <c>i</c> of the axis and point <c>i</c> of every series. Each handle is a native range input
/// (role <c>slider</c>), named by <see cref="StartLabel"/> or <see cref="EndLabel"/>, whose value text is
/// the name of its category: it moves with the pointer, a finger, or the keys of a slider (arrows by
/// one category, Page Up and Page Down by a larger step, Home and End to the bounds). The handles never
/// cross: the start stops at the end and the end at the start, so at least one category stays shown.
/// No script and no inline style: the overview is SVG geometry, the handles are styled by the sheet.
/// </remarks>
public partial class OmniRangeNavigator : IDisposable
{
    private int _start;
    private int? _end;
    private int? _givenStart;
    private int? _givenEnd;
    private bool _received;
    private IReadOnlyList<double> _totals = [];

    [CascadingParameter(Name = OmniChartContext.FooterName)] private OmniChartContext? ChartContext { get; set; }

    // The layout handed to the parts drawn in the chart: a navigator that receives it was written there.
    [CascadingParameter] private OmniChartContext? DrawingContext { get; set; }

    /// <summary>
    /// Index of the first category shown, bindable with <see cref="RangeStartChanged"/>. Null (the
    /// default) starts at the first category. A value out of the categories is brought back inside them.
    /// </summary>
    [Parameter] public int? RangeStart { get; set; }

    /// <summary>Raised with the index of the first category shown each time the start handle moves.</summary>
    [Parameter] public EventCallback<int?> RangeStartChanged { get; set; }

    /// <summary>
    /// Index of the last category shown, included, bindable with <see cref="RangeEndChanged"/>. Null
    /// (the default) ends at the last category, and follows it when categories are added. A value out
    /// of the categories, or before <see cref="RangeStart"/>, is brought back inside them.
    /// </summary>
    [Parameter] public int? RangeEnd { get; set; }

    /// <summary>Raised with the index of the last category shown each time the end handle moves.</summary>
    [Parameter] public EventCallback<int?> RangeEndChanged { get; set; }

    /// <summary>Accessible name of the group of handles; null (the default) takes the localized "Displayed range".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Accessible name of the start handle; null (the default) takes the localized "Start of the range".</summary>
    [Parameter] public string? StartLabel { get; set; }

    /// <summary>Accessible name of the end handle; null (the default) takes the localized "End of the range".</summary>
    [Parameter] public string? EndLabel { get; set; }

    /// <summary>The categories of the chart, the longest category axis or the longest series.</summary>
    private int Count => ChartContext is null
        ? 0
        : Math.Max(ChartContext.CategoryLabelCount, ChartContext.TableSeries.Select(item => item.Data.Count).DefaultIfEmpty(0).Max());

    private int Last => Math.Max(0, Count - 1);

    private int Start => Math.Clamp(_start, 0, Last);

    private int End => Math.Clamp(_end ?? Last, Start, Last);

    private IReadOnlyList<double> Totals => _totals;

    // Both handles on the same category in the right half: the start handle comes above the end one,
    // which could not move further right and would hide the only handle that can.
    private bool StartOnTop => Start == End && 2 * Start >= Last;

    private string ViewBox => FormattableString.Invariant($"0 0 {Math.Max(1, Last)} 100");

    /// <summary>Takes the range given by the host when it changed, then shows it in the chart.</summary>
    /// <exception cref="InvalidOperationException">The navigator is written among the parts drawn in a chart.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (DrawingContext is not null)
        {
            throw new InvalidOperationException(
                "OmniRangeNavigator is written in the FooterContent of its OmniChart, under the drawing, not among the parts drawn in it.");
        }

        // A value the host gives is taken when it differs from the one it gave before: a host that does
        // not bind the range keeps the handles where the reader left them.
        if (!_received || RangeStart != _givenStart)
        {
            _givenStart = RangeStart;
            _start = RangeStart ?? 0;
        }

        if (!_received || RangeEnd != _givenEnd)
        {
            _givenEnd = RangeEnd;
            _end = RangeEnd;
        }

        _received = true;
        Show();
    }

    /// <summary>Reads the overview of every category and hands the range to the chart, none when it covers them all.</summary>
    private void Show()
    {
        if (ChartContext is null)
        {
            return;
        }

        _totals = OmniChartRange.Totals(
            ChartContext.TableSeries.Where(item => !ChartContext.IsHidden(item.ColorIndex)).Select(item => item.Data),
            Count);

        // Every category selected is no range at all: the chart draws as it would without a navigator.
        if (Count == 0 || (Start == 0 && End == Last))
        {
            ChartContext.ClearRange(this);
            return;
        }

        ChartContext.SetRange(this, Start, End);
    }

    // Bound to the start handle: held between the first category and the end handle.
    private Task SetStartAsync(int value)
    {
        _start = Math.Clamp(value, 0, End);
        Show();
        return RangeStartChanged.InvokeAsync(_start);
    }

    // Bound to the end handle: held between the start handle and the last category.
    private Task SetEndAsync(int value)
    {
        var end = Math.Clamp(value, Start, Last);
        _end = end;
        Show();
        return RangeEndChanged.InvokeAsync(end);
    }

    /// <summary>The name of a category: its axis label, else the X of the first series that has it.</summary>
    /// <remarks>Only asked for a category of the chart (<see cref="Count"/> above zero), so the chart is there.</remarks>
    private string Name(int index) =>
        ChartContext!.CategoryLabel(index)
        ?? ChartContext.TableSeries.Where(item => index < item.Data.Count).Select(item => OmniChartGeometry.Display(item.Data[index].X)).FirstOrDefault()
        ?? OmniChartGeometry.Display(index + 1);

    /// <summary>The column of a category in the overview: from zero to its total, 96 high for the largest.</summary>
    private (double Y, double Height) Column(int index)
    {
        var low = Math.Min(0, _totals.Min());
        var high = Math.Max(0, _totals.Max());
        var span = high - low > 0 ? high - low : 1;
        double Y(double value) => 100 - ((value - low) / span * 96);
        var (zero, top) = (Y(0), Y(_totals[index]));
        return (Math.Min(zero, top), Math.Abs(zero - top));
    }

    /// <summary>
    /// The graduations under the overview: one every so many categories, so that no two names overlap
    /// (their width estimated as the category axis estimates it), the first and the last always. A name
    /// that would leave the overview is anchored to the edge it would cross.
    /// </summary>
    internal IReadOnlyList<(double X, double TextX, string? Anchor, string Text)> Ticks()
    {
        if (Count == 0)
        {
            return [];
        }

        var names = Enumerable.Range(0, Count).Select(Name).ToArray();
        var width = TicksWidth;
        var pitch = Last == 0 ? width : width / Last;
        var needed = (names.Max(name => name.Length) * OmniChartContext.CharacterWidth) + 1.5;
        var ticks = new List<(double, double, string?, string)>();
        foreach (var index in OmniChartRange.Thin(0, Last, Math.Max(1, (int)Math.Ceiling(needed / pitch))))
        {
            var x = Last == 0 ? width / 2 : index * pitch;
            var half = names[index].Length * OmniChartContext.CharacterWidth / 2;
            var (textX, anchor) = x - half < 0 ? (0d, (string?)null) : x + half > width ? (width, "end") : (x, "middle");
            ticks.Add((x, textX, anchor, names[index]));
        }

        return ticks;
    }

    // The graduations are drawn in the units of the chart's drawing, as wide as it (100 for a square
    // chart, more for a wide one), so their text matches the axis text of the chart. Only read while the
    // navigator has categories (Count above zero), so the chart is there.
    private double TicksWidth => ChartContext!.ViewWidth;

    private string TicksViewBox => FormattableString.Invariant($"0 0 {TicksWidth:0.###} 5");

    private string ColumnClass(int index) => index >= Start && index <= End
        ? "omni-range-navigator__column omni-range-navigator__column--selected"
        : "omni-range-navigator__column";

    private static string N(double value) => OmniChartGeometry.Number(value);

    /// <summary>Shows every category of the chart again.</summary>
    public void Dispose()
    {
        ChartContext?.ClearRange(this);
        GC.SuppressFinalize(this);
    }
}
