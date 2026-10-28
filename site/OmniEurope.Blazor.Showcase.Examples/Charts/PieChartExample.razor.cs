using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>A pie or donut chart whose kind, data, slice count, labels, legend and data table are parameters.</summary>
public partial class PieChartExample
{
    /// <summary>Whether the series is a full disc or a ring.</summary>
    [Parameter]
    public PieChartKind Kind { get; set; } = PieChartKind.Donut;

    /// <summary>The distribution drawn.</summary>
    [Parameter]
    public PieDataset Dataset { get; set; } = PieDataset.Channels;

    /// <summary>How many of the first slices of the distribution are kept.</summary>
    [Parameter]
    public int SliceCount { get; set; } = 5;

    /// <summary>
    /// Whether an empty share is added after the first slice: the series skips it without shifting the
    /// colours of the slices that follow.
    /// </summary>
    [Parameter]
    public bool IncludeZeroSlice { get; set; }

    /// <summary>Whether a legend names the slices, each with its share.</summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>Whether the values and shares are written in a table under the chart.</summary>
    [Parameter]
    public bool ShowDataTable { get; set; }

    /// <summary>What is written beside each slice, outside the disc, joined to it by a leader line.</summary>
    [Parameter]
    public OmniPieLabels OutsideLabels { get; set; } = OmniPieLabels.None;

    private IReadOnlyList<OmniChartSlice> SourceSlices => PieChartData.Of(Dataset);

    /// <summary>The slices shown: the first ones of the distribution, and the empty share when asked for.</summary>
    private IReadOnlyList<OmniChartSlice> Slices
    {
        get
        {
            var slices = SourceSlices.Take(Math.Clamp(SliceCount, 1, SourceSlices.Count)).ToList();
            if (IncludeZeroSlice)
            {
                slices.Insert(Math.Min(1, slices.Count), new OmniChartSlice("Empty share", 0));
            }

            return slices;
        }
    }

    private string ChartTitle => Dataset == PieDataset.Channels ? "Filings by channel" : "Files by country";

    /// <summary>One entry per slice drawn, with its share: the legend takes the palette in the order of the slices.</summary>
    private IReadOnlyList<string> LegendItems =>
        [.. Slices.Where(slice => slice.Value > 0).Select(slice => $"{slice.Label} ({FormatShare(slice.Value)})")];

    private static string FormatValue(double value) => value.ToString("N0", CultureInfo.CurrentCulture);

    private string FormatShare(double value)
    {
        var total = Slices.Sum(slice => slice.Value);
        return total <= 0 ? string.Empty : (value / total).ToString("P0", CultureInfo.CurrentCulture);
    }
}
