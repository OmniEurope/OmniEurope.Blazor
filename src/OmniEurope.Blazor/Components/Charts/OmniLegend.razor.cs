using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The legend of a chart: a swatch and a name for each series, or for each slice of a pie. Built from
/// the series of the chart by default (every series with a <c>Title</c>, in its own colour), so titles
/// and colours are written once, on the series.
/// </summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniLegend
{
    [Inject] private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>Accessible name of the legend; null (the default) takes the localized "Legend".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Entries written by the host instead of the names of the series. Entry <c>i</c> takes the colour
    /// of series <c>i</c> of the chart (colour <c>i</c> of the palette past the last series, or in a pie
    /// chart). Empty by default: the legend lists the series of the chart.
    /// </summary>
    [Parameter] public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Right of the plot, below the chart, above it, or <see cref="OmniLegendPosition.Auto"/> (the
    /// default): right while the longest entry fits a narrow column, below otherwise. Outside a chart
    /// the legend is always drawn on the right.
    /// </summary>
    [Parameter] public OmniLegendPosition Position { get; set; } = OmniLegendPosition.Auto;

    /// <summary>
    /// Whether a click on an entry that names a series hides that series, or shows it again: the chart
    /// redraws without it (axis, stacks and columns), the entry stays, dimmed and struck through. True by
    /// default (recette R-011); false keeps the legend a plain key. Below or above the chart each entry
    /// is a button (keyboard, <c>aria-pressed</c>); on the right, inside the drawing, which assistive
    /// technologies read as one image, an entry answers the pointer only, the data table staying the
    /// alternative. Pie slices and entries past the last series never toggle.
    /// </summary>
    [Parameter] public bool AllowToggle { get; set; } = true;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? StringLocalizer["LegendLabel"].Value : Label;

    private OmniChartContext.LegendRegistration Registration => new(EffectiveLabel, Items, Position, AllowToggle);

    /// <summary>What the legend draws: from the chart, or its own items in palette order outside one.</summary>
    private IReadOnlyList<OmniChartContext.LegendEntry> Entries =>
        ChartContext?.LegendEntries(Registration)
        ?? [.. Items.Select((text, index) => new OmniChartContext.LegendEntry(text, ChartColor.Slot(index), null))];

    /// <summary>Registers the legend, its label, items and position, with its chart; outside a chart, nothing to register.</summary>
    protected override void OnParametersSet() => ChartContext?.RegisterLegend(this, Registration);

    private bool Outside => ChartContext?.IsLegendOutside(this) == true;

    // The legend column right of the plot; it moves with the plot when the chart is wide.
    private double LegendLeft => ChartContext?.LegendLeft ?? 79;

    private static string N(double value) => OmniChartGeometry.Number(value);

    private bool CanToggle(OmniChartContext.LegendEntry entry) =>
        ChartContext is not null && OmniChartContext.CanToggle(Registration, entry);

    private string EntryClass(OmniChartContext.LegendEntry entry) => CssClassBuilder.Combine([
        "omni-chart__legend-entry",
        CanToggle(entry) ? "omni-chart__legend-entry--toggle" : null,
        ChartContext?.IsEntryHidden(entry) == true ? "omni-chart__legend-entry--hidden" : null]);

    private void Toggle(OmniChartContext.LegendEntry entry)
    {
        if (CanToggle(entry))
        {
            ChartContext!.ToggleSeries(entry.SeriesColor!.Value);
        }
    }

    /// <summary>Removes the legend from its chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterLegend(this);
        GC.SuppressFinalize(this);
    }
}
